# This source code is licensed under the MIT license found in the
# LICENSE file in the root directory of this open-source project.


""" Define the dance dataset. """
import numpy as np
import torch
import torch.utils.data
from torch.utils.data import Dataset
import pdb


def paired_collate_fn(insts):
    # for src in insts:
    #     for s in src:
    #         print(s.shape)

    # print()

    mo_seq = list(zip(*insts))
    print(mo_seq.shape)
    mo_seq = torch.FloatTensor(mo_seq)
    print('Here!')
    print(mo_seq.size())

    return mo_seq


class MoSeq(Dataset):
    def __init__(self, dances):
        self.dances = dances

    def __len__(self):
        return len(self.dances)

    def __getitem__(self, index):
        # print(self.dances[index].shape)
        return self.dances[index]
