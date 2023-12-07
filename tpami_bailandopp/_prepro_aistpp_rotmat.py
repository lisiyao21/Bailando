# This source code is licensed under the MIT license found in the
# LICENSE file in the root directory of this open-source project.


import os
import sys
import json
import random
import argparse
import essentia
import essentia.streaming
from essentia.standard import *
import librosa
import numpy as np
from extractor import FeatureExtractor
from aistplusplus_api.aist_plusplus.loader import AISTDataset
from smplx import SMPL
import torch
from scipy.spatial.transform import Rotation as R

parser = argparse.ArgumentParser()
parser.add_argument('--input_video_dir', type=str, default='aist_plusplus_final/all_musics')
parser.add_argument('--input_annotation_dir', type=str, default='aist_plusplus_final')
parser.add_argument('--smpl_dir', type=str, default='smpl')

parser.add_argument('--train_dir', type=str, default='data/aistpp_train_wav_rotmat')
parser.add_argument('--test_dir', type=str, default='data/aistpp_test_full_wav_rotmat')

parser.add_argument('--split_train_file', type=str, default='aist_plusplus_final/splits/crossmodal_train.txt')
parser.add_argument('--split_test_file', type=str, default='aist_plusplus_final/splits/crossmodal_test.txt')
parser.add_argument('--split_val_file', type=str, default='aist_plusplus_final/splits/crossmodal_val.txt')

parser.add_argument('--sampling_rate', type=int, default=15360*2)
args = parser.parse_args()

extractor = FeatureExtractor()

if not os.path.exists(args.train_dir):
    os.mkdir(args.train_dir)
if not os.path.exists(args.test_dir):
    os.mkdir(args.test_dir)

split_train_file = args.split_train_file
split_test_file = args.split_test_file
split_val_file = args.split_val_file

def make_music_dance_set(video_dir, annotation_dir):
    print('---------- Extract features from raw audio ----------')
    # print(annotation_dir)
    aist_dataset = AISTDataset(annotation_dir) 

    musics = []
    dances = []
    fnames = []
    train = []
    test = []

    # music_dance_keys = []

    # onset_beats = []
    audio_fnames = sorted(os.listdir(video_dir))
    # dance_fnames = sorted(os.listdir(dance_dir))
    # audio_fnames = audio_fnames[:20]  # for debug
    # print(f'audio_fnames: {audio_fnames}')

    train_file = open(split_train_file, 'r')
    for fname in train_file.readlines():
        train.append(fname.strip())
    train_file.close()

    test_file = open(split_test_file, 'r')
    for fname in test_file.readlines():
        test.append(fname.strip())
    test_file.close()

    test_file = open(split_val_file, 'r')
    for fname in test_file.readlines():
        test.append(fname.strip())
    test_file.close()

    ii = 0
    all_names = train + test
    for audio_fname in all_names:
        # if ii > 1:
        #     break
        # ii += 1
        video_file = os.path.join(video_dir, audio_fname.split('_')[4] + '.wav')
        print(f'Process -> {video_file}')
        print(audio_fname)
        seq_name, _ = AISTDataset.get_seq_name(audio_fname.replace('cAll', 'c02'))
        
        if (seq_name not in train) and (seq_name not in test):
            print(f'Not in set!')
            continue

        if seq_name in fnames:
            print(f'Already scaned!')
            continue

        sr = args.sampling_rate
        
        loader = None
        try:
            loader = essentia.standard.MonoLoader(filename=video_file, sampleRate=sr)
        except RuntimeError:
            continue

        fnames.append(seq_name)
        print(seq_name)
        
        ### load audio features ###

    
        audio = loader()
        audio = np.array(audio).T

        feature =  extract_acoustic_feature(audio, sr)
        musics.append(feature.tolist())

        ### load pose sequence ###
       # for seq_name in tqdm(seq_names):
        print(f'Process -> {seq_name}')
        # smpl_poses, smpl_scaling, smpl_trans = AISTDataset.load_motion(
        #     aist_dataset.motion_dir, seq_name)
        # smpl = None
        # smpl = SMPL(model_path=args.smpl_dir, gender='MALE', batch_size=1)
        # keypoints3d = smpl.forward(
        #     global_orient=torch.from_numpy(smpl_poses[:, 0:1]).float(),
        #     body_pose=torch.from_numpy(smpl_poses[:, 1:]).float(),
        #     transl=torch.from_numpy(smpl_trans / smpl_scaling).float(),
        #     ).joints.detach().numpy()[:, 0:24, :]
        # nframes = keypoints3d.shape[0]
        # dances.append(keypoints3d.reshape(nframes, -1).tolist())
        # print(np.shape(dances[-1]))  # (nframes, 72)


        smpl_poses, smpl_scaling, smpl_trans = AISTDataset.load_motion(
            aist_dataset.motion_dir, seq_name)
        smpl_trans = smpl_trans / smpl_scaling
        nframes = smpl_poses.shape[0]
        njoints = 24

        r = R.from_rotvec(smpl_poses.reshape([nframes*njoints, 3])) 
        rotmat = r.as_dcm().reshape([nframes, njoints, 3, 3])

        rotmat = np.concatenate([
            smpl_trans,
            rotmat.reshape([nframes, njoints * 3 * 3])
        ], axis=-1)
        nframes = rotmat.shape[0]
        dances.append(rotmat.reshape(nframes, -1).tolist())

        print(np.shape(dances[-1]))  # (nframes, 3 + 24 * 9)

        

    # return None, None, None
    return musics, dances, fnames


def extract_acoustic_feature(audio, sr):

    melspe_db = extractor.get_melspectrogram(audio, sr)
    
    mfcc = extractor.get_mfcc(melspe_db)
    mfcc_delta = extractor.get_mfcc_delta(mfcc)
    # mfcc_delta2 = get_mfcc_delta2(mfcc)

    audio_harmonic, audio_percussive = extractor.get_hpss(audio)
    # harmonic_melspe_db = get_harmonic_melspe_db(audio_harmonic, sr)
    # percussive_melspe_db = get_percussive_melspe_db(audio_percussive, sr)
    chroma_cqt = extractor.get_chroma_cqt(audio_harmonic, sr, octave=7 if sr==15360*2 else 5)
    # chroma_stft = extractor.get_chroma_stft(audio_harmonic, sr)

    onset_env = extractor.get_onset_strength(audio_percussive, sr)
    tempogram = extractor.get_tempogram(onset_env, sr)
    onset_beat = extractor.get_onset_beat(onset_env, sr)[0]
    # onset_tempo, onset_beat = librosa.beat.beat_track(onset_envelope=onset_env, sr=sr)
    # onset_beats.append(onset_beat)

    onset_env = onset_env.reshape(1, -1)

    feature = np.concatenate([
        # melspe_db,
        mfcc, # 20
        mfcc_delta, # 20
        # mfcc_delta2,
        # harmonic_melspe_db,
        # percussive_melspe_db,
        # chroma_stft,
        chroma_cqt, # 12
        onset_env, # 1
        onset_beat, # 1
        tempogram
    ], axis=0)

            # mfcc, #20
            # mfcc_delta, #20

            # chroma_cqt, #12
            # onset_env, # 1
            # onset_beat, #1

    feature = feature.transpose(1, 0)
    print(f'acoustic feature -> {feature.shape}')

    return feature

def align(musics, dances):
    print('---------- Align the frames of music and dance ----------')
    assert len(musics) == len(dances), \
        'the number of audios should be equal to that of videos'
    new_musics=[]
    new_dances=[]
    for i in range(len(musics)):
        min_seq_len = min(len(musics[i]), len(dances[i]))
        print(f'music -> {np.array(musics[i]).shape}, ' +
              f'dance -> {np.array(dances[i]).shape}, ' +
              f'min_seq_len -> {min_seq_len}')

        new_musics.append([musics[i][j] for j in range(min_seq_len)])
        new_dances.append([dances[i][j] for j in range(min_seq_len)])

    return new_musics, new_dances, musics



def split_data(fnames):
    train = []
    test = []

    print('---------- Split data into train and test ----------')

    print(fnames)
    
    train_file = open(split_train_file, 'r')
    for fname in train_file.readlines():
        train.append(fnames.index(fname.strip()))
    train_file.close()

    test_file = open(split_test_file, 'r')
    for fname in test_file.readlines():
        test.append(fnames.index(fname.strip()))
    test_file.close()

    test_file = open(split_val_file, 'r')
    for fname in test_file.readlines():
        test.append(fnames.index(fname.strip()))
    test_file.close()

    train = np.array(train)
    test = np.array(test)

    return train, test


def save(args, musics, dances, fnames, musics_raw):
    print('---------- Save to text file ----------')
    # fnames = sorted(os.listdir(os.path.join(args.input_dance_dir,inner_dir)))
    # # fnames = fnames[:20]  # for debug
    # assert len(fnames)*2 == len(musics) == len(dances), 'alignment'

    # fnames = sorted(fnames)
    train_idx, test_idx = split_data(fnames)
    # train_idx = sorted(train_idx)
    print(f'train ids: {[fnames[idx] for idx in train_idx]}')
    # test_idx = sorted(test_idx)
    print(f'test ids: {[fnames[idx] for idx in test_idx]}')

    print('---------- train data ----------')

    for idx in train_idx:
        with open(os.path.join(args.train_dir, f'{fnames[idx]}.json'), 'w') as f:
            sample_dict = {
                'id': fnames[idx],
                'music_array': musics[idx],
                'dance_array': dances[idx]
            }
            # print(sample_dict)
            json.dump(sample_dict, f)

    print('---------- test data ----------')
    for idx in test_idx:
        with open(os.path.join(args.test_dir, f'{fnames[idx]}.json'), 'w') as f:
            sample_dict = {
                'id': fnames[idx],
                'music_array': musics_raw[idx], # musics[idx+i],
                'dance_array': dances[idx]
            }
            # print(sample_dict)
            json.dump(sample_dict, f)



if __name__ == '__main__':
    musics, dances, fnames = make_music_dance_set(args.input_video_dir, args.input_annotation_dir) 

    musics, dances, musics_raw = align(musics, dances)
    save(args, musics, dances, fnames, musics_raw)


