""" Define the functions to load data. """
import os
import json
import argparse
import numpy as np
import time
import pdb
import numpy

from PIL import Image
from keypoint2img import read_keypoints
from multiprocessing import Pool
from functools import partial
from tqdm import tqdm
import pickle
import cv2

from smplx import SMPL


from scipy.spatial.transform import Rotation as R

import os
import shutil

filePath = './test/'
savePath = './data_'

pose_keypoints_num = 25
face_keypoints_num = 70
hand_left_keypoints_num = 21
hand_right_keypoints_num = 21

class VSConfig():
    height = 540
    width = 960*2

def write2txt(dances, dance_names, config, expdir, epoch):
    epoch = int(epoch)
    assert len(dances) == len(dance_names),\
        "number of generated dance != number of dance_names"
    

    ep_path = os.path.join(expdir, "txts", f"ep{epoch:06d}")
        
    if not os.path.exists(ep_path):
        os.makedirs(ep_path)


    # print("Writing TxT...")
    for i in tqdm(range(len(dances)),desc='Generating Txts'):
        num_poses = dances[i].shape[0]
        dances[i] = dances[i].reshape(num_poses, pose_keypoints_num, 3)
        dance_path = os.path.join(ep_path, dance_names[i])
        if not os.path.exists(dance_path):
            os.makedirs(dance_path)

        for j in range(num_poses):
            frame_dict = {'version': 1.2}
            # 2-D key points
            pose_keypoints_2d = []
            frame_txt = np.zeros((17,3))

            keypoints = dances[i][j]
            for k, keypoint in enumerate(keypoints):
                x = (keypoint[0] + 1) * 0.5 * config.width
                y = (keypoint[1] + 1) * 0.5 * config.height
                z = (keypoint[2] + 1) * 0.5 * config.height
                score = 0.8
                if k < pose_keypoints_num:
                    pose_keypoints_2d.extend([x, y, z])
            #         # pose_keypoints_2d.extend([x, y, score])
            pose_keypoints_2d = np.array(pose_keypoints_2d).reshape(25,3)

            frame_txt[0] = pose_keypoints_2d[8]

            frame_txt[1] = pose_keypoints_2d[12]
            frame_txt[2] = pose_keypoints_2d[13]
            frame_txt[3] = pose_keypoints_2d[14]
            frame_txt[4] = pose_keypoints_2d[9]
            frame_txt[5] = pose_keypoints_2d[10]
            frame_txt[6] = pose_keypoints_2d[11]

            if pose_keypoints_2d[1][0] > pose_keypoints_2d[8][0]:
                frame_txt[7][0] = pose_keypoints_2d[1][0] - np.abs(pose_keypoints_2d[1][0]-pose_keypoints_2d[8][0])/2
            else:
                frame_txt[7][0] = pose_keypoints_2d[8][0] - np.abs(pose_keypoints_2d[1][0]-pose_keypoints_2d[8][0])/2

            if pose_keypoints_2d[1][2] > pose_keypoints_2d[8][2]:
                frame_txt[7][2] = pose_keypoints_2d[1][2] - np.abs(pose_keypoints_2d[1][2]-pose_keypoints_2d[8][2])/2
            else:
                frame_txt[7][2] = pose_keypoints_2d[8][2] - np.abs(pose_keypoints_2d[1][2]-pose_keypoints_2d[8][2])/2
            
            frame_txt[8] = pose_keypoints_2d[1]
            frame_txt[9] = pose_keypoints_2d[0]
            frame_txt[10] = pose_keypoints_2d[15]
            frame_txt[11] = pose_keypoints_2d[2]
            frame_txt[12] = pose_keypoints_2d[3]
            frame_txt[13] = pose_keypoints_2d[4]
            frame_txt[14] = pose_keypoints_2d[5]
            frame_txt[15] = pose_keypoints_2d[6]
            frame_txt[16] = pose_keypoints_2d[7]
            frame_txt = np.transpose(frame_txt).tolist()
            frame_txt = [frame_txt]
            with open(os.path.join(dance_path, f'{j}.txt'), 'w') as f:
                f.writelines("%s" % frame_txt)


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

def visualize_json(fname_iter, image_dir, dance_name, dance_path, config, quant=None):
    j, fname = fname_iter
    json_file = os.path.join(dance_path, fname)
    img = Image.fromarray(read_keypoints(json_file, (config.width, config.height),
                                         remove_face_labels=False, basic_point_only=False))
    img = img.transpose(Image.Transpose.FLIP_TOP_BOTTOM)
    
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
        for j in range(len(fnames)):
            visualize_json((j, fnames[j]), image_dir=image_dir, dance_name=dance_name, dance_path=dance_path, config=config, quant=quant_list)
        # pool = Pool(worker_num)
        # partial_func = partial(visualize_json, image_dir=image_dir,
        #                        dance_name=dance_name, dance_path=dance_path, config=config, quant=quant_list)
        # pool.map(partial_func, enumerate(fnames))
        # pool.close()
        # pool.join()

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
        
        dimension = 3

        np_dance = np_dance.reshape([b, 24, 3])
        # b = min(b, 900)
        np_dance = np_dance[:b]
        np_dance -= np_dance[:1, :1, :]
        np_dance2 = np_dance[:, :, :dimension] / 1.5
        np_dance2[:, :, 0] /= 2.2
        # np_dance2[:, :, 1] += 0.5
        # np_dance2 = np_dance[:, :, :2]
        # np_dance2 = np_dance
        # b = 900
        np_dance_trans = np.zeros([b, 25, dimension]).copy()
        
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

        np_dances.append(np_dance_trans.reshape([b, 25*dimension]))
    
    # write2pkl(dance_datas, dance_names, config.testing, expdir, epoch, rotmat=config.rotmat)
    # write2json(np_dances, dance_names,config, pkl_root, 123221)
    write2txt(np_dances, dance_names,config, pkl_root, 123221)
    # visualize(config, dance_names, pkl_root, 123221, quants=None)
    # img2video(pkl_root, 123221)

    json_dir = os.path.join(pkl_root, "jsons",f"ep{123221:06d}")
    img_dir = os.path.join(pkl_root, "imgs",f"ep{123221:06d}")
    # if os.path.exists(json_dir):    
    #     shutil.rmtree(json_dir)
    if os.path.exists(img_dir):
        shutil.rmtree(img_dir)

visualizeAndWritefromPKL(filePath)

# dataList = []
# k = 0
# tmpPath = savePath+str(k)
# if not os.path.exists(tmpPath):
#     os.makedirs(tmpPath)

# for i in range(dataList[k]['keypoints3d_optim'].shape[0]):
#     tmpData = np.transpose(dataList[k]['keypoints3d_optim'][i])
#     tmpData = tmpData.tolist()
#     tmpData = [tmpData]

#     tmpPath = savePath+str(k)
#     if not os.path.exists(tmpPath):
#         os.makedirs(tmpPath)
    
#     with open(tmpPath+'/'+str(i)+'.txt','w') as f:
#         f.writelines("%s" % tmpData)
