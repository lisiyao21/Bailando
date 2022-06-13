# This source code is licensed under the MIT license found in the
# LICENSE file in the root directory of this open-source project.


""" Define the functions to load data. """
import os
import json
import argparse
import numpy as np
import torch
import time
import pdb
import numpy

from PIL import Image
from .keypoint2img import read_keypoints
from multiprocessing import Pool
from functools import partial
from tqdm import tqdm
import pickle
import cv2

from smplx import SMPL


from scipy.spatial.transform import Rotation as R

import os
import shutil

def eye(n, batch_shape):
    iden = np.zeros(np.concatenate([batch_shape, [n, n]]))
    iden[..., 0, 0] = 1.0
    iden[..., 1, 1] = 1.0
    iden[..., 2, 2] = 1.0
    return iden


def rotmat2aa(rotmats):
    """
    Convert rotation matrices to angle-axis using opencv's Rodrigues formula.
    Args:
        rotmats: A np array of shape (..., 3, 3)
    Returns:
        A np array of shape (..., 3)
    """
    assert rotmats.shape[-1] == 3 and rotmats.shape[-2] == 3 and len(rotmats.shape) >= 3, 'invalid input dimension'
    orig_shape = rotmats.shape[:-2]
    rots = np.reshape(rotmats, [-1, 3, 3])
    r = R.from_dcm(rots)  # from_matrix
    aas = r.as_rotvec()
    return np.reshape(aas, orig_shape + (3,))


def get_closest_rotmat(rotmats):
    """
    Finds the rotation matrix that is closest to the inputs in terms of the Frobenius norm. For each input matrix
    it computes the SVD as R = USV' and sets R_closest = UV'. Additionally, it is made sure that det(R_closest) == 1.
    Args:
        rotmats: np array of shape (..., 3, 3).
    Returns:
        A numpy array of the same shape as the inputs.
    """
    u, s, vh = np.linalg.svd(rotmats)
    r_closest = np.matmul(u, vh)

    # if the determinant of UV' is -1, we must flip the sign of the last column of u
    det = np.linalg.det(r_closest)  # (..., )
    iden = eye(3, det.shape)
    iden[..., 2, 2] = np.sign(det)
    r_closest = np.matmul(np.matmul(u, iden), vh)
    return r_closest


pose_keypoints_num = 25
face_keypoints_num = 70
hand_left_keypoints_num = 21
hand_right_keypoints_num = 21



def generate(model, src_aud, src_aud_pos, src_pos, src_pos_pos):
    """ Generate dance pose in one batch """
    with torch.no_grad():

        # attention: AIST++这篇paper将2s的动作也作为输入，所以需要滚动预测

        bsz, src_seq_len, _ = src_aud.size()
        _, _ , pose_dim = src_pos.size()

        # 前120帧作为输入
        generated_frames_num = src_seq_len - 120
        
        # 像后面补充120帧保证预测完整
        # for ii in range(120):
        src_aud = torch.cat([src_aud, src_aud[:, :120]], dim=1)
        
        # 输出motion先加上前120帧motion
        out_seq = src_pos.clone()

        


        for i in range(0, generated_frames_num, 1):

            output = model(src_aud[:, i:i+240], src_aud_pos[:, :240], out_seq[:, i:i+120], src_pos_pos[:, :240])

            if generated_frames_num - i < 1:
                print('the last frame!')
                output = output[:, :1]
            else:
                output = output[:, :1]
            out_seq = torch.cat([out_seq, output], 1)

    out_seq = out_seq[:, :].view(bsz, -1, pose_dim)

    return out_seq

def img2video(expdir, epoch, audio_path=None):
    video_dir = os.path.join(expdir, "videos",f"ep{epoch:06d}")
    if not os.path.exists(video_dir):
        os.makedirs(video_dir)

    image_dir = os.path.join(expdir, "imgs", f"ep{epoch:06d}")


    dance_names = sorted(os.listdir(image_dir))
    audio_dir = "aist_plusplus_final/all_musics"
    
    music_names = sorted(os.listdir(audio_dir))
    
    for dance in tqdm(dance_names, desc='Generating Videos'):
        #pdb.set_trace()
        name = dance.split(".")[0]
        cmd = f"ffmpeg -r 60 -i {image_dir}/{dance}/frame%06d.png -vb 20M -vcodec mpeg4 -y {video_dir}/{name}.mp4 -loglevel quiet"
        # cmd = f"ffmpeg -r 60 -i {image_dir}/{dance}/%05d.png -vb 20M -vcodec qtrle -y {video_dir}/{name}.mov -loglevel quiet"

        os.system(cmd)
        
        name1 = name.replace('cAll', 'c02')

        if 'cAll' in name:
            music_name = name[-9:-5] + '.wav'
        else:
            music_name = name + '.mp3'
            audio_dir = 'extra/'
            music_names = sorted(os.listdir(audio_dir))
        
        if music_name in music_names:
            print('combining audio!')
            audio_dir_ = os.path.join(audio_dir, music_name)
            print(audio_dir_)
            name_w_audio = name + "_audio"
            cmd_audio = f"ffmpeg -i {video_dir}/{name}.mp4 -i {audio_dir_} -map 0:v -map 1:a -c:v copy -shortest -y {video_dir}/{name_w_audio}.mp4 -loglevel quiet"
            os.system(cmd_audio)



def visualize_json(fname_iter, image_dir, dance_name, dance_path, config, quant=None):
    j, fname = fname_iter
    json_file = os.path.join(dance_path, fname)
    img = Image.fromarray(read_keypoints(json_file, (config.width, config.height),
                                         remove_face_labels=False, basic_point_only=False))
    img = img.transpose(Image.FLIP_TOP_BOTTOM)
    img = np.asarray(img)
    if quant is not None:
        cv2.putText(img, str(quant[j]), (config.width-400, 80), cv2.FONT_HERSHEY_SIMPLEX, 1, (0,255,0), 3)
    img = cv2.cvtColor(img, cv2.COLOR_BGR2BGRA)
    # img[np.all(img == [0, 0, 0, 255], axis=2)] = [255, 255, 255, 0]
    img = Image.fromarray(numpy.uint8(img))
    img.save(os.path.join(f'{image_dir}/{dance_name}', f'frame{j:06d}.png'))


def visualize(config, the_dance_names, expdir, epoch, quants=None, worker_num=16):
    epoch = int(epoch)
    json_dir = os.path.join(expdir, "jsons",f"ep{epoch:06d}")

    image_dir = os.path.join(expdir, "imgs", f"ep{epoch:06d}")

    if not os.path.exists(image_dir):
        os.makedirs(image_dir)

    dance_names = sorted(os.listdir(json_dir))
    dance_names = the_dance_names


        # print(quants)
    quant_list = None

    # print("Visualizing")
    for i, dance_name in enumerate(tqdm(dance_names, desc='Generating Images')):
        dance_path = os.path.join(json_dir, dance_name)
        fnames = sorted(os.listdir(dance_path))
        if not os.path.exists(f'{image_dir}/{dance_name}'):
            os.makedirs(f'{image_dir}/{dance_name}')
        if quants is not None:
            if isinstance(quants[dance_name], tuple):
                quant_lists = []
                for qs in quants[dance_name]:   
                    downsample_rate = max(len(fnames) // len(qs), 1)
                    quant_lists.append(qs.repeat(downsample_rate).tolist())
                quant_list = [tuple(qlist[ii] for qlist in quant_lists) for ii in range(len(quant_lists[0]))]
            # while len(quant_list) < len(dance_names):
            # print(quants)
            # print(len(fnames), len(quants[dance_name]))
            else:                
                downsample_rate = max(len(fnames) // len(quants[dance_name]), 1)
                quant_list = quants[dance_name].repeat(downsample_rate).tolist()
            while len(quant_list) < len(dance_names):
                quant_list.append(quant_list[-1])        


        # Visualize json in parallel
        pool = Pool(worker_num)
        partial_func = partial(visualize_json, image_dir=image_dir,
                               dance_name=dance_name, dance_path=dance_path, config=config, quant=quant_list)
        pool.map(partial_func, enumerate(fnames))
        pool.close()
        pool.join()
        
        
def write2json_original(dances, dance_names, config, expdir, epoch):
    epoch = int(epoch)
    assert len(dances) == len(dance_names),\
        "number of generated dance != number of dance_names"
    
    if not os.path.exists(os.path.join(expdir, "jsons_original")):
        os.makedirs(os.path.join(expdir, "jsons_original"))

    ep_path = os.path.join(expdir, "jsons_original", f"ep{epoch:06d}")
        
    if not os.path.exists(ep_path):
        os.makedirs(ep_path)


    # print("Writing Json...")
    for i in tqdm(range(len(dances)),desc='Generating Jsons'):
        num_poses = dances[i].shape[0]
        dances[i] = dances[i].reshape(num_poses, 24, 3)
        dance_path = os.path.join(ep_path, dance_names[i])
        if not os.path.exists(dance_path):
            os.makedirs(dance_path)

        for j in range(num_poses):
            frame_dict = {'3d_pose': dances[i][j].tolist()}
            frame_json = json.dumps(frame_dict)
            with open(os.path.join(dance_path, f'ep{epoch:06d}_frame{j:06d}_kps.json'), 'w') as f:
                f.write(frame_json)

def write2pkl(dances, dance_names, config, expdir, epoch, rotmat):
    epoch = int(epoch)
    # print(len(dances))
    # print(len(dance_names))
    assert len(dances) == len(dance_names),\
        "number of generated dance != number of dance_names"
    
    if not os.path.exists(os.path.join(expdir, "pkl")):
        os.makedirs(os.path.join(expdir, "pkl"))

    ep_path = os.path.join(expdir, "pkl", f"ep{epoch:06d}")
        
    if not os.path.exists(ep_path):
        os.makedirs(ep_path)


    # print("Writing Json...")
    for i in tqdm(range(len(dances)),desc='Generating Jsons'):

        # if rotmat:
        #     mat, trans = dances[i]
        #     pkl_data = {"pred_motion": mat, "pred_trans": trans}
        # else:
        np_dance = dances[i]
        pkl_data = {"pred_position": np_dance}

        dance_path = os.path.join(ep_path, dance_names[i] + '.pkl')
        # if not os.path.exists(dance_path):
        #     os.makedirs(dance_path)

        # with open(dance_path, 'w') as f:
        np.save(dance_path, pkl_data)


def pose_code2pkl(pcodes, dance_names, config, expdir, epoch):
    epoch = int(epoch)
    # print(len(pcodes))
    # print(len(dance_names))
    assert len(pcodes) == len(dance_names),\
        "number of generated dance != number of dance_names"
    
    if not os.path.exists(os.path.join(expdir, "pose_codes")):
        os.makedirs(os.path.join(expdir, "pose_codes"))

    ep_path = os.path.join(expdir, "pose_codes", f"ep{epoch:06d}")
        
    if not os.path.exists(ep_path):
        os.makedirs(ep_path)


    # print("Writing Json...")
    for i in tqdm(range(len(pcodes)),desc='writing pose code'):

        # if rotmat:
        #     mat, trans = dances[i]
        #     pkl_data = {"pred_motion": mat, "pred_trans": trans}
        # else:

        name = dance_names[i]
        pcode = pcodes[name]

        
        pkl_data = {"pcodes_up": pcode[0], "pcodes_down": pcode[1]}

        dance_path = os.path.join(ep_path, name + '.pkl')
        # if not os.path.exists(dance_path):
        #     os.makedirs(dance_path)

        # with open(dance_path, 'w') as f:
        np.save(dance_path, pkl_data)

def write2json(dances, dance_names, config, expdir, epoch):
    epoch = int(epoch)
    assert len(dances) == len(dance_names),\
        "number of generated dance != number of dance_names"
    

    ep_path = os.path.join(expdir, "jsons", f"ep{epoch:06d}")
        
    if not os.path.exists(ep_path):
        os.makedirs(ep_path)


    # print("Writing Json...")
    for i in tqdm(range(len(dances)),desc='Generating Jsons'):
        num_poses = dances[i].shape[0]
        dances[i] = dances[i].reshape(num_poses, pose_keypoints_num, 2)
        dance_path = os.path.join(ep_path, dance_names[i])
        if not os.path.exists(dance_path):
            os.makedirs(dance_path)

        for j in range(num_poses):
            frame_dict = {'version': 1.2}
            # 2-D key points
            pose_keypoints_2d = []
            # Random values for the below key points
            face_keypoints_2d = []
            hand_left_keypoints_2d = []
            hand_right_keypoints_2d = []
            # 3-D key points
            pose_keypoints_3d = []
            face_keypoints_3d = []
            hand_left_keypoints_3d = []
            hand_right_keypoints_3d = []

            keypoints = dances[i][j]
            for k, keypoint in enumerate(keypoints):
                x = (keypoint[0] + 1) * 0.5 * config.width
                y = (keypoint[1] + 1) * 0.5 * config.height
                score = 0.8
                if k < pose_keypoints_num:
                    pose_keypoints_2d.extend([x, y, score])
                elif k < pose_keypoints_num + face_keypoints_num:
                    face_keypoints_2d.extend([x, y, score])
                elif k < pose_keypoints_num + face_keypoints_num + hand_left_keypoints_num:
                    hand_left_keypoints_2d.extend([x, y, score])
                else:
                    hand_right_keypoints_2d.extend([x, y, score])

            people_dicts = []
            people_dict = {'pose_keypoints_2d': pose_keypoints_2d,
                           'face_keypoints_2d': face_keypoints_2d,
                           'hand_left_keypoints_2d': hand_left_keypoints_2d,
                           'hand_right_keypoints_2d': hand_right_keypoints_2d,
                           'pose_keypoints_3d': pose_keypoints_3d,
                           'face_keypoints_3d': face_keypoints_3d,
                           'hand_left_keypoints_3d': hand_left_keypoints_3d,
                           'hand_right_keypoints_3d': hand_right_keypoints_3d}
            people_dicts.append(people_dict)
            frame_dict['people'] = people_dicts
            frame_json = json.dumps(frame_dict)
            with open(os.path.join(dance_path, f'ep{epoch:06d}_frame{j:06d}_kps.json'), 'w') as f:
                f.write(frame_json)
        

def visualizeAndWrite(results,config,expdir,dance_names, epoch, quants=None):
    if config.rotmat:
        smpl = SMPL(model_path=config.smpl_dir, gender='MALE', batch_size=1)
    np_dances = []
    np_dances_original = []
    dance_datas = []
    if config.data.name == "aist":

        for i in range(len(results)):
            np_dance = results[i][0].data.cpu().numpy()

            if config.rotmat:
                print('Use SMPL!')
                root = np_dance[:, :3]
                rotmat = np_dance[:, 3:].reshape([-1, 3, 3])


                # write2pkl((rotmat, root), dance_names[i], config.testing, expdir, epoch, rotmat=True)

                rotmat = get_closest_rotmat(rotmat)
                smpl_poses = rotmat2aa(rotmat).reshape(-1, 24, 3)
                np_dance = smpl.forward(
                    global_orient=torch.from_numpy(smpl_poses[:, 0:1]).float(),
                    body_pose=torch.from_numpy(smpl_poses[:, 1:]).float(),
                    transl=torch.from_numpy(root).float(),
                ).joints.detach().numpy()[:, 0:24, :]
                b = np_dance.shape[0]
                np_dance = np_dance.reshape(b, -1)
                dance_datas.append(np_dance)
            # print(np_dance.shape)
            else:
                # if args.use_mean_pose:
                #     print('We use mean pose!')
                #     np_dance += mean_pose
                root = np_dance[:, :3]
                np_dance = np_dance + np.tile(root, (1, 24))
                np_dance[:, :3] = root

                
                dance_datas.append(np_dance)
                # write2pkl(np_dance, dance_names[i], config.testing, expdir, epoch, rotmat=True)

            root = np_dance[:, :3]
            # np_dance = np_dance + np.tile(root, (1, 24))
            np_dance[:, :3] = root
            # np_dance[2:-2] = (np_dance[:-4] + np_dance[1:-3] + np_dance[2:-2] +  np_dance[3:-1] + np_dance[4:]) / 5.0
            np_dances_original.append(np_dance)

            b, c = np_dance.shape
            np_dance = np_dance.reshape([b, c//3, 3])
            # np_dance2 = np_dance[:, :, :2] / 2 - 0.5
            # np_dance2[:, :, 1] = np_dance2[:, :, 1]
            np_dance2 = np_dance[:, :, :2] / 1.5
            np_dance2[:, :, 0] /= 2.2
            np_dance_trans = np.zeros([b, 25, 2]).copy()
            
            # head
            np_dance_trans[:, 0] = np_dance2[:, 12]
            
            #neck
            np_dance_trans[:, 1] = np_dance2[:, 9]
            
            # left up
            np_dance_trans[:, 2] = np_dance2[:, 16]
            np_dance_trans[:, 3] = np_dance2[:, 18]
            np_dance_trans[:, 4] = np_dance2[:, 20]

            # right up
            np_dance_trans[:, 5] = np_dance2[:, 17]
            np_dance_trans[:, 6] = np_dance2[:, 19]
            np_dance_trans[:, 7] = np_dance2[:, 21]

            
            np_dance_trans[:, 8] = np_dance2[:, 0]
            
            np_dance_trans[:, 9] = np_dance2[:, 1]
            np_dance_trans[:, 10] = np_dance2[:, 4]
            np_dance_trans[:, 11] = np_dance2[:, 7]

            np_dance_trans[:, 12] = np_dance2[:, 2]
            np_dance_trans[:, 13] = np_dance2[:, 5]
            np_dance_trans[:, 14] = np_dance2[:, 8]

            np_dance_trans[:, 15] = np_dance2[:, 15]
            np_dance_trans[:, 16] = np_dance2[:, 15]
            np_dance_trans[:, 17] = np_dance2[:, 15]
            np_dance_trans[:, 18] = np_dance2[:, 15]

            np_dance_trans[:, 19] = np_dance2[:, 11]
            np_dance_trans[:, 20] = np_dance2[:, 11]
            np_dance_trans[:, 21] = np_dance2[:, 8]

            np_dance_trans[:, 22] = np_dance2[:, 10]
            np_dance_trans[:, 23] = np_dance2[:, 10]
            np_dance_trans[:, 24] = np_dance2[:, 7]

            np_dances.append(np_dance_trans.reshape([b, 25*2]))
    else:
        for i in range(len(results)):
            np_dance = results[i][0].data.cpu().numpy()
            root = np_dance[:, 2*8:2*9]
            np_dance = np_dance + np.tile(root, (1, 25))
            np_dance[:, 2*8:2*9] = root
            np_dances.append(np_dance)
    write2pkl(dance_datas, dance_names, config.testing, expdir, epoch, rotmat=config.rotmat)
    pose_code2pkl(quants, dance_names, config.testing, expdir, epoch)
    write2json(np_dances, dance_names,config.testing, expdir,epoch)
    visualize(config.testing, dance_names, expdir,epoch, quants)
    img2video(expdir,epoch)

    json_dir = os.path.join(expdir, "jsons",f"ep{epoch:06d}")
    img_dir = os.path.join(expdir, "imgs",f"ep{epoch:06d}")
    if os.path.exists(json_dir):    
        shutil.rmtree(json_dir)
    # if os.path.exists(img_dir):
    #     shutil.rmtree(img_dir)

def load_data(data_dir, interval=900, data_type='2D'):
    music_data, dance_data = [], []
    fnames = sorted(os.listdir(data_dir))
    # print(fnames)
    # fnames = fnames[:10]  # For debug
    if ".ipynb_checkpoints" in fnames:
        fnames.remove(".ipynb_checkpoints")
    for fname in fnames:
        path = os.path.join(data_dir, fname)
        with open(path) as f:
            sample_dict = json.loads(f.read())
            np_music = np.array(sample_dict['music_array'])
            np_dance = np.array(sample_dict['dance_array'])
            if data_type == '2D':
                # Only use 25 keypoints (x,y) skeleton (basic bone) for 2D
                np_dance = np_dance[:, :50]
                root = np_dance[:, 2*8:2*9]  # Use the hip keyjoint as the root
                np_dance = np_dance - np.tile(root, (1, 25))  # Calculate relative offset with respect to root
                np_dance[:, 2*8:2*9] = root

            if interval is not None:
                seq_len, dim = np_music.shape
                for i in range(0, seq_len, interval):
                    music_sub_seq = np_music[i: i + interval]
                    dance_sub_seq = np_dance[i: i + interval]
                    if len(music_sub_seq) == interval:
                        music_data.append(music_sub_seq)
                        dance_data.append(dance_sub_seq)
            else:
                music_data.append(np_music)
                dance_data.append(np_dance)

    return music_data, dance_data
    # , [fn.replace('.json', '') for fn in fnames]


def load_data_aist(data_dir, interval=120, move=40, rotmat=False, external_wav=None, external_wav_rate=1, music_normalize=False, wav_padding=0):
    tot = 0
    music_data, dance_data = [], []
    fnames = sorted(os.listdir(data_dir))
    # print(fnames)
    # fnames = fnames[:10]  # For debug
    
    if ".ipynb_checkpoints" in fnames:
        fnames.remove(".ipynb_checkpoints")
    for fname in fnames:
        path = os.path.join(data_dir, fname)
        with open(path) as f:
            # print(path)
            sample_dict = json.loads(f.read())
            np_music = np.array(sample_dict['music_array'])

            if external_wav is not None:
                wav_path = os.path.join(external_wav, fname.split('_')[-2] + '.json')
                # print('load from external wav!')
                with open(wav_path) as ff:
                    sample_dict_wav = json.loads(ff.read())
                    np_music = np.array(sample_dict_wav['music_array']).astype(np.float32)
                    
            
            np_dance = np.array(sample_dict['dance_array'])

            if not rotmat:
                root = np_dance[:, :3]  # the root
                np_dance = np_dance - np.tile(root, (1, 24))  # Calculate relative offset with respect to root
                np_dance[:, :3] = root

            music_sample_rate = external_wav_rate if external_wav is not None else 1
            # print('music_sample_rate', music_sample_rate)
            # print(music_sample_rate)
            if interval is not None:
                seq_len, dim = np_music.shape
                for i in range(0, seq_len, move):
                    i_sample = i // music_sample_rate
                    interval_sample = interval // music_sample_rate

                    music_sub_seq = np_music[i_sample: i_sample + interval_sample]
                    dance_sub_seq = np_dance[i: i + interval]

                    if len(music_sub_seq) == interval_sample and len(dance_sub_seq) == interval:
                        padding_sample = wav_padding // music_sample_rate
                        # Add paddings/context of music
                        music_sub_seq_pad = np.zeros((interval_sample + padding_sample * 2, dim), dtype=music_sub_seq.dtype)
                        
                        if padding_sample > 0:
                            music_sub_seq_pad[padding_sample:-padding_sample] = music_sub_seq
                            start_sample = padding_sample if i_sample > padding_sample else i_sample
                            end_sample = padding_sample if i_sample + interval_sample + padding_sample < seq_len else seq_len - (i_sample + interval_sample)
                            # print(end_sample)
                            music_sub_seq_pad[padding_sample - start_sample:padding_sample] = np_music[i_sample - start_sample:i_sample]
                            if end_sample == padding_sample:
                                music_sub_seq_pad[-padding_sample:] = np_music[i_sample + interval_sample:i_sample + interval_sample + end_sample]
                            else:     
                                music_sub_seq_pad[-padding_sample:-padding_sample + end_sample] = np_music[i_sample + interval_sample:i_sample + interval_sample + end_sample]
                        else:
                            music_sub_seq_pad = music_sub_seq
                        music_data.append(music_sub_seq_pad)
                        dance_data.append(dance_sub_seq)
                        tot += 1
                        # if tot > 1:
                        #     break
            else:
                music_data.append(np_music)
                dance_data.append(np_dance)

            # if tot > 1:
            #     break
            
            # tot += 1
            # if tot > 100:
            #     break
    music_np = np.stack(music_data).reshape(-1, music_data[0].shape[1])
    music_mean = music_np.mean(0)
    music_std = music_np.std(0)
    music_std[(np.abs(music_mean) < 1e-5) & (np.abs(music_std) < 1e-5)] = 1
    
    # music_data_norm = [ (music_sub_seq - music_mean) / (music_std + 1e-10) for music_sub_seq in music_data ]
    # print(music_np)

    if music_normalize:
        print('calculating norm mean and std')
        music_data_norm = [ (music_sub_seq - music_mean) / (music_std + 1e-10) for music_sub_seq in music_data ]
        with open('/mnt/lustressd/lisiyao1/dance_experiements/music_norm.json', 'w') as fff:
            sample_dict = {
                'music_mean': music_mean.tolist(), # musics[idx+i],
                'music_std': music_std.tolist()
            }
            # print(sample_dict)
            json.dump(sample_dict, fff)
    else:
        music_data_norm = music_data 


    return music_data_norm, dance_data, ['11', '22',]
    # , [fn.replace('.json', '') for fn in fnames]


def load_test_data(data_dir, data_type='2D'):
    tot = 0
    music_data, dance_data = [], []
    fnames = sorted(os.listdir(data_dir))
    print(fnames)
    # fnames = fnames[:60]  # For debug
    for fname in fnames:
        path = os.path.join(data_dir, fname)
        with open(path) as f:
            sample_dict = json.loads(f.read())
            np_music = np.array(sample_dict['music_array'])
            np_dance = np.array(sample_dict['dance_array'])
            if data_type == '2D':
                # Only use 25 keypoints skeleton (basic bone) for 2D
                np_dance = np_dance[:, :50]
                root = np_dance[:, 2*8:2*9]
                np_dance = np_dance - np.tile(root, (1, 25))
                np_dance[:, 2*8:2*9] = root

            music_data.append(np_music)
            dance_data.append(np_dance)


    return music_data, dance_data, fnames



def load_test_data_aist(data_dir, rotmat, move, external_wav=None, external_wav_rate=1, music_normalize=False, wav_padding=0):

    tot = 0
    input_names = []

    music_data, dance_data = [], []
    fnames = sorted(os.listdir(data_dir))
    #print(fnames)
    # fnames = fnames[:60]  # For debug
    for fname in fnames:
        path = os.path.join(data_dir, fname)
        with open(path) as f:
            #print(path)
            sample_dict = json.loads(f.read())
            np_music = np.array(sample_dict['music_array'])
            if external_wav is not None:
                # print('load from external wav!')
                wav_path = os.path.join(external_wav, fname.split('_')[-2] + '.json')
                with open(wav_path) as ff:
                    sample_dict_wav = json.loads(ff.read())
                    np_music = np.array(sample_dict_wav['music_array'])
            
            if 'dance_array' in sample_dict:
                np_dance = np.array(sample_dict['dance_array'])
                if not rotmat:
                    root = np_dance[:, :3]  # the root
                    np_dance = np_dance - np.tile(root, (1, 24))  # Calculate relative offset with respect to root
                    np_dance[:, :3] = root

                for kk in range((len(np_dance) // move + 1) * move - len(np_dance) ):
                    np_dance = np.append(np_dance, np_dance[-1:], axis=0)

                dance_data.append(np_dance)
            # print('Music data shape: ', np_music.shape)
            else:
                np_dance = None
                dance_data = None
            music_move = external_wav_rate if external_wav is not None else move
            
            # zero padding left
            for kk in range(wav_padding):
                np_music = np.append(np.zeros_like(np_music[-1:]), np_music, axis=0)
            # fully devisable
            for kk in range((len(np_music) // music_move + 1) * music_move - len(np_music) ):
                np_music = np.append(np_music, np_music[-1:], axis=0)
            # zero padding right
            for kk in range(wav_padding):
                np_music = np.append(np_music, np.zeros_like(np_music[-1:]), axis=0)

            music_data.append(np_music)
            input_names.append(fname)
            # tot += 1
            # if tot == 3:
            #     break
    # if music_normalize:
    if False:
        with open('/mnt/lustressd/lisiyao1/dance_experiements/music_norm.json') as fff:
            sample_dict = json.loads(fff.read())
            music_mean = np.array(sample_dict['music_mean'])
            music_std = np.array(sample_dict['music_std'])
        music_std[(np.abs(music_mean) < 1e-5) & (np.abs(music_std) < 1e-5)] = 1

        music_data_norm = [ (music_sub_seq - music_mean) / (music_std + 1e-10) for music_sub_seq in music_data ]
    else:
        music_data_norm = music_data

    return music_data_norm, dance_data, input_names
    


def load_json_data(data_file, max_seq_len=150):
    music_data = []
    dance_data = []
    count = 0
    total_count = 0
    with open(data_file) as f:
        data_list = json.loads(f.read())
        for data in data_list:
            # The first and last segment may be unusable
            music_segs = data['music_segments']
            dance_segs = data['dance_segments']

            assert len(music_segs) == len(dance_segs), 'alignment'

            for i in range(len(music_segs)):
                total_count += 1
                if len(music_segs[i]) > max_seq_len:
                    count += 1
                    continue
                music_data.append(music_segs[i])
                dance_data.append(dance_segs[i])

    rate = count / total_count
    print(f'total num of segments: {total_count}')
    print(f'num of segments length > {max_seq_len}: {count}')
    print(f'the rate: {rate}')

    return music_data, dance_data


def str2bool(v):
    if v.lower() in ('yes', 'true', 't', 'y', '1'):
        return True
    elif v.lower() in ('no', 'false', 'f', 'n', '0'):
        return False
    else:
        raise argparse.ArgumentTypeError('Boolean value expected.')



def check_data_distribution(data_dir, interval=240, rotmat=False):
    tot = 0
    music_data, dance_data = [], []
    fnames = sorted(os.listdir(data_dir))
    # print(fnames)
    # fnames = fnames[:10]  # For debug
    
    dance_mean = []
    dance_std = []
    dance_max = []
    dance_min = []
    music_mean = []
    music_std = []
    music_max = []
    music_min = []  


    if ".ipynb_checkpoints" in fnames:
        fnames.remove(".ipynb_checkpoints")
    for fname in fnames:
        path = os.path.join(data_dir, fname)
        with open(path) as f:
            # print(path)
            sample_dict = json.loads(f.read())
            np_music = np.array(sample_dict['music_array'])
            np_dance = np.array(sample_dict['dance_array'])

            if not rotmat:
                root = np_dance[:, :3]  # the root
                np_dance = np_dance - np.tile(root, (1, 24))  # Calculate relative offset with respect to root
                np_dance[:, :3] = root

            
            if interval is not None:
                seq_len, dim = np_music.shape
                for i in range(0, seq_len, interval):
                    music_sub_seq = np_music[i: i + interval]
                    dance_sub_seq = np_dance[i: i + interval]
                    if len(music_sub_seq) == interval:
                        
                        music_mean.append(music_sub_seq.mean(0))
                        music_std.append(np.std(music_sub_seq, axis=0))
                        music_max.append(music_sub_seq.max(0))
                        music_min.append(music_sub_seq.min(0))

                        dance_mean.append(dance_sub_seq.mean(0))
                        dance_std.append(np.std(dance_sub_seq, axis=0))
                        dance_max.append(dance_sub_seq.max(0))
                        dance_min.append(dance_sub_seq.min(0))
            # np_dance = np_dance.reshape(-1, 24, 3)

    music_mean = np.array(music_mean).mean(0)
    music_std =  np.array(music_std).mean(0)

    dance_mean = np.array(dance_mean).mean(0)
    dance_std = np.array(dance_std).mean(0)

    # print(dance_mean, dance_std, dance_max, dance_min)
    return dance_mean, dance_std, music_mean, music_std,
    # , [fn.replace('.json', '') for fn in fnames]

class VSConfig():
    height = 540
    width = 960*2

def visualizeAndWritefromPKL(pkl_root, config=None):
    if config is None:
        config = VSConfig()
    dance_names = []
    np_dances = []
    np_dances_original = []
    dance_datas = []
    for pkl_name in os.listdir(pkl_root):
        if os.path.isdir(os.path.join(pkl_root, pkl_name)):
            continue
        result = np.load(os.path.join(pkl_root, pkl_name), allow_pickle=True).item()['pred_position']
        dance_names.append(pkl_name)

        np_dance = result

        root = np_dance[:, :3]
        # np_dance = np_dance - np.tile(root, (1, 24))
        np_dance[:, :3] = root
        np_dances_original.append(np_dance)

        if len(np_dance.shape) == 2:
            b, c = np_dance.shape
        else:
            np_dance = np_dance[:, :24]
            b, c, _ = np_dance.shape
        # print(np_dance.shape)
        
        np_dance = np_dance.reshape([b, 24, 3])
        # b = min(b, 900)
        np_dance = np_dance[:b]
        np_dance -= np_dance[:1, :1, :]
        np_dance2 = np_dance[:, :, :2] / 1.5
        np_dance2[:, :, 0] /= 2.2
        # np_dance2[:, :, 1] += 0.5
        # np_dance2 = np_dance[:, :, :2]
        # np_dance2 = np_dance
        # b = 900
        np_dance_trans = np.zeros([b, 25, 2]).copy()
        
        # head
        np_dance_trans[:, 0] = np_dance2[:, 12]
        
        #neck
        np_dance_trans[:, 1] = np_dance2[:, 9]
        
        # left up
        np_dance_trans[:, 2] = np_dance2[:, 16]
        np_dance_trans[:, 3] = np_dance2[:, 18]
        np_dance_trans[:, 4] = np_dance2[:, 20]

        # right up
        np_dance_trans[:, 5] = np_dance2[:, 17]
        np_dance_trans[:, 6] = np_dance2[:, 19]
        np_dance_trans[:, 7] = np_dance2[:, 21]

        
        np_dance_trans[:, 8] = np_dance2[:, 0]
        
        np_dance_trans[:, 9] = np_dance2[:, 1]
        np_dance_trans[:, 10] = np_dance2[:, 4]
        np_dance_trans[:, 11] = np_dance2[:, 7]

        np_dance_trans[:, 12] = np_dance2[:, 2]
        np_dance_trans[:, 13] = np_dance2[:, 5]
        np_dance_trans[:, 14] = np_dance2[:, 8]

        np_dance_trans[:, 15] = np_dance2[:, 15]
        np_dance_trans[:, 16] = np_dance2[:, 15]
        np_dance_trans[:, 17] = np_dance2[:, 15]
        np_dance_trans[:, 18] = np_dance2[:, 15]

        np_dance_trans[:, 19] = np_dance2[:, 11]
        np_dance_trans[:, 20] = np_dance2[:, 11]
        np_dance_trans[:, 21] = np_dance2[:, 8]

        np_dance_trans[:, 22] = np_dance2[:, 10]
        np_dance_trans[:, 23] = np_dance2[:, 10]
        np_dance_trans[:, 24] = np_dance2[:, 7]

        np_dances.append(np_dance_trans.reshape([b, 25*2]))
    
    # write2pkl(dance_datas, dance_names, config.testing, expdir, epoch, rotmat=config.rotmat)
    write2json(np_dances, dance_names,config, pkl_root, 123221)
    visualize(config, dance_names, pkl_root, 123221, quants=None)
    img2video(pkl_root, 123221)

    json_dir = os.path.join(pkl_root, "jsons",f"ep{123221:06d}")
    img_dir = os.path.join(pkl_root, "imgs",f"ep{123221:06d}")
    if os.path.exists(json_dir):    
        shutil.rmtree(json_dir)
    if os.path.exists(img_dir):
        shutil.rmtree(img_dir)

def npy2pkl(npy_file, pkl_root):
    if not os.path.exists(pkl_root):
        os.mkdir(pkl_root)
    alls = np.load(npy_file, allow_pickle=True).item()
    print(len(alls.keys()))
    for pkl in alls.keys():
    # print(alls.keys())
    # while True:
    #     pass
    # while True:
        # if os.path.isdir(os.path.join(root, pkl)):
        #     continue
        # pklo = pkl
        pkls = pkl.replace('c01', 'cAll')
        joint3d = alls[pkl]['kps3d'][:]
        pkl_data = {"pred_position": joint3d}
        dance_path = os.path.join(pkl_root, pkls + '.pkl')
        # if not os.path.exists(dance_path):
        #     os.makedirs(dance_path)

        # with open(dance_path, 'w') as f:
        np.save(dance_path, pkl_data)

        
        
def pkl_to_19point(pkl_root, config=None):
    if config is None:
        config = VSConfig()
    dance_names = []
    np_dances = []
    np_dances_original = []
    dance_datas = []
    if not os.path.exists(os.path.join(pkl_root, '19points')):
        os.mkdir(os.path.join(pkl_root, '19points'))
    for pkl_name in os.listdir(pkl_root):
        print(pkl_name)

        if os.path.isdir(os.path.join(pkl_root, pkl_name)):
            continue
        result = np.load(os.path.join(pkl_root, pkl_name), allow_pickle=True).item()['pred_position']
        dance_names.append(pkl_name)

        np_dance = result

        root = np_dance[:, :3]
        # np_dance = np_dance + np.tile(root, (1, 24))
        np_dance[:, :3] = root
        np_dances_original.append(np_dance)

        if len(np_dance.shape) == 2:
            b, c = np_dance.shape
        else:
            b, c, _ = np_dance.shape
        # np_dance = np_dance.reshape([b, c//3, 3])
        # np_dance2 = np_dance[:, :, :2] / 2 - 0.5
        # np_dance2[:, :, 1] = np_dance2[:, :, 1]
        np_dance2 = np_dance.reshape(b, 24, 3)
        np_dance2[:, :, 1:] *= -1
        np_dance_trans = np.zeros([b, 19, 3]).copy()
        
        # head
        np_dance_trans[:, 0] = np_dance2[:, 0]
        np_dance_trans[:, 1] = np_dance2[:, 2]
        np_dance_trans[:, 2] = np_dance2[:, 5]
        np_dance_trans[:, 3] = np_dance2[:, 8]

        np_dance_trans[:, 4] = np_dance2[:, 1]
        np_dance_trans[:, 5] = np_dance2[:, 4]
        np_dance_trans[:, 6] = np_dance2[:, 7]

        np_dance_trans[:, 7] = np_dance2[:, 6]
        np_dance_trans[:, 8] = np_dance2[:, 12]
        np_dance_trans[:, 9] = np_dance2[:, 15]
        np_dance_trans[:, 10] =  np_dance2[:, 12] + 1.7 * (np_dance2[:, 12] - np_dance2[:, 6])

        np_dance_trans[:, 11] = np_dance2[:, 16]
        np_dance_trans[:, 12] = np_dance2[:, 18]
        np_dance_trans[:, 13] = np_dance2[:, 20]
        np_dance_trans[:, 14] = np_dance2[:, 17]
        np_dance_trans[:, 15] = np_dance2[:, 19]
        np_dance_trans[:, 16] = np_dance2[:, 21]
        np_dance_trans[:, 17] = np_dance2[:, 11]
        np_dance_trans[:, 18] = np_dance2[:, 10]
        # np_dance_trans[:, 0] = np_dance2[:, 0]

        
        # #neck
        # np_dance_trans[:, 1] = np_dance2[:, 9]
        
        # # left up
        # np_dance_trans[:, 2] = np_dance2[:, 16]
        # np_dance_trans[:, 3] = np_dance2[:, 18]
        # np_dance_trans[:, 4] = np_dance2[:, 20]

        # # right up
        # np_dance_trans[:, 5] = np_dance2[:, 17]
        # np_dance_trans[:, 6] = np_dance2[:, 19]
        # np_dance_trans[:, 7] = np_dance2[:, 21]

        
        # np_dance_trans[:, 8] = np_dance2[:, 0]
        
        # np_dance_trans[:, 9] = np_dance2[:, 1]
        # np_dance_trans[:, 10] = np_dance2[:, 4]
        # np_dance_trans[:, 11] = np_dance2[:, 7]

        # np_dance_trans[:, 12] = np_dance2[:, 2]
        # np_dance_trans[:, 13] = np_dance2[:, 5]
        # np_dance_trans[:, 14] = np_dance2[:, 8]

        # np_dance_trans[:, 15] = np_dance2[:, 15]
        # np_dance_trans[:, 16] = np_dance2[:, 15]
        # np_dance_trans[:, 17] = np_dance2[:, 15]
        # np_dance_trans[:, 18] = np_dance2[:, 15]

        # np_dance_trans[:, 19] = np_dance2[:, 11]
        # np_dance_trans[:, 20] = np_dance2[:, 11]
        # np_dance_trans[:, 21] = np_dance2[:, 8]

        # np_dance_trans[:, 22] = np_dance2[:, 10]
        # np_dance_trans[:, 23] = np_dance2[:, 10]
        # np_dance_trans[:, 24] = np_dance2[:, 7]

        # np_dances.append(np_dance_trans.reshape([b, 25*2]))
        with open(os.path.join(pkl_root, '19points', pkl_name + '.txt'), 'w+') as file:
            for tt in range(len(np_dance_trans)):
                for jj in range(len(np_dance_trans[0])):
                    for kk in range(3):
                        file.write(str(np_dance_trans[tt][jj][kk].item()))
                        if ((jj != len(np_dance_trans[0]) - 1) or (kk != 2)):
                            file.write(' ')
                        else:
                            file.write('\n')

    # write2pkl(dance_datas, dance_names, config.testing, expdir, epoch, rotmat=config.rotmat)
    # write2json(np_dances, dance_names,config, pkl_root, 123221)
    # visualize(config, dance_names, pkl_root, 123221, quants=None)
    # img2video(pkl_root,12321)

    # json_dir = os.path.join(pkl_root, "jsons",f"ep{epoch:06d}")
    # img_dir = os.path.join(pkl_root, "imgs",f"ep{epoch:06d}")
    # if os.path.exists(json_dir):    
    #     shutil.rmtree(json_dir)

# def main():
#     config = {'height': 1280, 'width': 720, 'ckpt_epoch': 10}

#     visualize()





# if __name__ == '__main__':
#     main()
