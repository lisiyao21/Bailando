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


class MoDaSeq(Dataset):
    def __init__(self, musics, dances=None):
        if dances is not None:
            assert (len(musics) == len(dances)), \
                'the number of dances should be equal to the number of musics'
        self.musics = musics
        self.dances = dances
        # if clip_names is not None:
        # self.clip_names = clip_names

    def __len__(self):
        return len(self.musics)

    def __getitem__(self, index):
        if self.dances is not None:
            # if self.clip_names is not None:
            #     return self.musics[index], self.dances[index], self.clip_names[index]
            # else:
            return self.musics[index], self.dances[index],
        else:
            return self.musics[index]
