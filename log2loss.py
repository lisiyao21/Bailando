# from motion_vqvae import MoQ
import argparse
# import os
# import yaml
# from pprint import pprint
from easydict import EasyDict
import matplotlib.pyplot as plt
import datetime
import numpy as np

def parse_args():
    parser = argparse.ArgumentParser()
    # parser.add_argument('--config', default='')
    # exclusive arguments
    # group = parser.add_mutually_exclusive_group(required=True)
    parser.add_argument('--log', default='log.txt')
    parser.add_argument('--store_path', default='.')
    parser.add_argument('--threshold', type=float, default=np.inf)

    return parser.parse_args()


def main():
    iters = []
    losses = []
    # parse arguments and load config
    
    args = parse_args()
    f = open(args.log, 'r')
    for ss in f.readlines():
        if ss.find('update') < 0:
            continue
        else:
            sps = ss.split(' ')
            iter = 0
            loss = 0
            for sp in sps:
                if 'update' in sp:
                    iter = int(sp[sp.find('updates[') + len('updates[') : -1])
                if 'loss[' in sp:
                    loss = float(sp[sp.find('loss[') + len('loss[') : -1]) 
            if iter < len(iters):
                losses[iter] = min(loss, args.threshold)
            else:
                losses.append( min(loss, args.threshold))
                iters.append(iter)
    plt.plot(iters, losses, 'b-')
    # plt.legend()
    plt.xlabel(u'iters')
    plt.ylabel(u'loss')
    plt.title('Training loss')
    plt.savefig( args.store_path + '/' + datetime.datetime.now().strftime('%Y-%m-%d') + '_loss.jpg')


if __name__ == '__main__':
    main()
