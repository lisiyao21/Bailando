# This source code is licensed under the MIT license found in the
# LICENSE file in the root directory of this open-source project.


""" Define the dance dataset. """
import numpy as np
import torch
import torch.utils.data
from torch.utils.data import Dataset


def paired_collate_fn(insts):
    src_seq, tgt_seq, name = list(zip(*insts))
    src_pos = np.array([
        [pos_i + 1 for pos_i, v_i in enumerate(inst)] for inst in src_seq])

    src_seq = torch.FloatTensor(src_seq)
    src_pos = torch.LongTensor(src_pos)
    tgt_seq = torch.FloatTensor(tgt_seq)

    return src_seq, src_pos, tgt_seq, name



class MoDaSeqAC(Dataset):
    def __init__(self, musics, dances, beats, interval=None, music_dance_ratio=1, wav_padding=0):
        # if dances is not None:
        ups, downs = dances
        assert (len(musics) == (len(ups))), \
            'the number of dances should be equal to the number of musics'

        music_data = []
        dance_data_up = []
        dance_data_down = []
        beat_data = []
        mask_data = []

        ups, downs = dances
        tot = 0

        for (np_music, np_dance_up, np_dance_down, beat) in zip(musics, ups, downs, beats):
            # print('Here', np_music, interval, flush=True)
            if wav_padding > 0:
                np_music = np_music[wav_padding:-wav_padding, :]
            if interval is not None:

                seq_len, dim = np_music.shape
                # print(seq_len, dim)
                # print(np_dance_up.shape)
                # print(np_dance_down.shape)
                # print(seq_len)
                # print(interval)
                # print(seq_len - interval)
                # print(beat.shape)

                
                for i in range(0, seq_len//music_dance_ratio-interval+1):
                    if i == 0:
                        mask = np.ones([interval - 2], dtype=np.float32)
                    else:
                        mask = np.zeros([interval - 2], dtype=np.float32)
                        mask[-1] = 1.0


                    i_sample = int(i * music_dance_ratio)
                    interval_sample = int(interval * music_dance_ratio)

                    music_sub_seq = np_music[i_sample: i_sample + interval_sample]
                    dance_sub_seq_up = np_dance_up[i: i + interval]
                    dance_sub_seq_down = np_dance_down[i: i + interval]
                    beat_this = beat[i*8:i*8+interval*8]
                    if len(beat_this) is not 8*interval:
                        for iii in range(8*interval - len(beat_this)):
                            beat_this = np.append(beat_this, beat_this[-1:])

                    if len(music_sub_seq) == interval_sample and len(dance_sub_seq_up) == interval:
                        
                        padding_sample = wav_padding
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
                        dance_data_up.append(dance_sub_seq_up)
                        dance_data_down.append(dance_sub_seq_down)
                        beat_data.append(beat_this)
                        mask_data.append(mask)

                        tot += 1
                        # print('True!')
        print(tot)
        self.musics = music_data
        self.dances_up = dance_data_up
        self.dances_down = dance_data_down
        self.beat_data = beat_data
        self.mask_data = mask_data
        # if clip_names is not None:
        # self.clip_names = clip_names

    def __len__(self):
        return len(self.musics)

    def __getitem__(self, index):
        # if self.dances is not None:
            # if self.clip_names is not None:
            #     return self.musics[index], self.dances[index], self.clip_names[index]
            # else:
        return self.musics[index], self.dances_up[index], self.dances_down[index], self.beat_data[index], self.mask_data[index]
        # else:
        #     return self.musics[index]
