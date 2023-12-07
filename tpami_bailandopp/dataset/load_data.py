import json
import numpy as np
import os


def load_train_data_aist(cfg):
    data_dir = cfg.data_dir
    interval = cfg.seq_len
    move = cfg.move
    rotmat = cfg.rotmat
    external_wav= cfg.external_wav if hasattr(cfg, 'external_wav') else None
    external_wav_rate = cfg.external_wav_rate if hasattr(cfg, 'external_wav_rate') else None


    tot = 0
    music_data, dance_data, input_names = [], [], []
    
    # traverse all data
    fnames = sorted(os.listdir(data_dir))

    for fname in fnames:
        
        path = os.path.join(data_dir, fname)
        with open(path) as f:

            sample_dict = json.loads(f.read())
            np_music = np.array(sample_dict['music_array']).astype(np.float32)

            if external_wav is not None:
                wav_path = os.path.join(external_wav, fname.split('_')[-2] + '.json')

                with open(wav_path) as ff:
                    sample_dict_wav = json.loads(ff.read())
                    np_music = np.array(sample_dict_wav['music_array']).astype(np.float32)
                    print(np_music.shape, flush=True)
        
            np_dance = np.array(sample_dict['dance_array']).astype(np.float32)

            if not rotmat:
                root = np_dance[:, :3]  # the root
                np_dance = np_dance - np.tile(root, (1, 24))  # Calculate relative offset with respect to root
                np_dance[:, :3] = root
                

            music_sample_rate = external_wav_rate if external_wav is not None else 1

            if interval is not None: # just sample a piece of music
                seq_len = np_music.shape[0]
                for i in range(0, seq_len, move):
                    i_sample = i // music_sample_rate
                    interval_sample = interval // music_sample_rate

                    music_sub_seq = np_music[i_sample: i_sample + interval_sample]
                    dance_sub_seq = np_dance[i: i + interval]

                    if len(music_sub_seq) == interval_sample and len(dance_sub_seq) == interval:
                        music_sub_seq_pad = music_sub_seq
                        music_data.append(music_sub_seq_pad)
                        dance_data.append(dance_sub_seq)
                        input_names.append(fname)
                        tot += 1
                        

            else:
                music_data.append(np_music)
                dance_data.append(np_dance)
                input_names.append(fname)


    return music_data, dance_data, input_names


def load_test_data_aist(cfg):

    data_dir = cfg.data_dir
    move = cfg.move 
    rotmat = cfg.rotmat
    external_wav= cfg.external_wav if hasattr(cfg, 'external_wav') else None
    external_wav_rate = cfg.external_wav_rate if hasattr(cfg, 'external_wav_rate') else None

    music_data, dance_data, input_names = [], [], []
    fnames = sorted(os.listdir(data_dir))

    for fname in fnames:
        path = os.path.join(data_dir, fname)
        with open(path) as f:

            sample_dict = json.loads(f.read())
            np_music = np.array(sample_dict['music_array']).astype(np.float32)
            if external_wav is not None: # using music features from external files
                wav_path = os.path.join(external_wav, fname.split('_')[-2] + '.json')
                with open(wav_path) as ff:
                    sample_dict_wav = json.loads(ff.read())
                    np_music = np.array(sample_dict_wav['music_array']).astype(np.float32)
            
            if 'dance_array' in sample_dict:
                np_dance = np.array(sample_dict['dance_array']).astype(np.float32)
                if not rotmat:
                    root = np_dance[:, :3]  # the root
                    np_dance = np_dance - np.tile(root, (1, 24))  # Calculate relative offset with respect to root
                    np_dance[:, :3] = root

                for kk in range((len(np_dance) // move + 1) * move - len(np_dance) ):
                    np_dance = np.append(np_dance, np_dance[-1:], axis=0)

                dance_data.append(np_dance)
            else:
                np_dance = None
                dance_data = None
            music_move = external_wav_rate if external_wav is not None else move
            
            # fully devisable
            for kk in range((len(np_music) // music_move + 1) * music_move - len(np_music)):
                np_music = np.append(np_music, np_music[-1:], axis=0)

            music_data.append(np_music)
            input_names.append(fname)

    return music_data, dance_data, input_names
    
