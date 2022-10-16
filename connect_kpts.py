import numpy as np
import argparse
import json, io, pickle, os
from PIL import Image
import cv2, copy

def parse_args():
    parser = argparse.ArgumentParser()
    parser.add_argument('--kpt-dir', type=str, default='D:\\CILAB\\과제\\CT\\청각\\ZICO-Summer_Hate_accompaniment.json')
    parser.add_argument('--hand-dir', type=str, default='D:\\CILAB\\과제\\CT\\청각\\수정된키포인트')
    parser.add_argument('--save-dir', type=str, default='./test')
    return parser.parse_args()

class ConnectKptHand:

    def __init__(self, kpt_dir, hand_dir, save_dir, kpt_scaling=1.4, hand_scaling=0.5):
        # paths
        self.kpt_dir = kpt_dir
        self.hand_dir = hand_dir
        self.save_dir = save_dir
        if not os.path.exists(save_dir):
            os.makedirs(save_dir)
        # joint connection
        self.adj_hands = [[0, 1], [1, 2], [2, 3], [3, 4], [0, 5], [5, 6], [6, 7], [7, 8],
                    [5, 9], [9, 10], [10, 11], [11, 12], [9, 13], [13, 14], [14, 15],
                    [15, 16], [13, 17], [0, 17], [17, 18], [18, 19], [19, 20]]
        self.adj_kpts = [
            [0, 1], [1, 8],  # body
            [1, 2], [2, 3], [3, 4],  # right arm
            [1, 5], [5, 6], [6, 7],  # left arm
            [8, 9], [9, 10], [10, 11], [11, 24], [11, 22], [22, 23],  # right leg
            [8, 12], [12, 13], [13, 14], [14, 21], [14, 19], [19, 20]]  # left leg

        self.kpt_scaling = kpt_scaling
        self.hand_scaling = hand_scaling
        self.img_shape = (1080, 1920)

    def read_data(self):
        kpt_files = os.listdir(self.kpt_dir)
        self.kpts_seq = np.zeros((len(kpt_files)//2, 25, 3))
        idx = 0
        for i, kpt_file in enumerate(kpt_files):
            if i % 2 == 0:
                continue
            with io.open(os.path.join(self.kpt_dir, kpt_file), 'rb') as f:
                jd = json.load(f)
                dance = np.array(jd['people'][0]['pose_keypoints_2d'])
                dance = np.reshape(dance, (25, 3))
            self.kpts_seq[idx] = dance
            idx += 1

        hand_files = os.listdir(self.hand_dir)
        self.hands_seq = np.zeros((len(hand_files), 42, 3))
        for j, hand_file in enumerate(hand_files):
            with io.open(os.path.join(self.hand_dir, hand_file), 'rb') as f2:
                jd2 = json.load(f2)
                hand = np.array(jd2['keypoints_3d'])
            if len(hand) == 21:
                self.hands_seq[j, :21] = hand
            else:
                self.hands_seq[j] = hand

    def _match_hand(self, kpt_hands, hands):
        diff = hands[0] - kpt_hands
        hands -= diff
        return hands

    def draw_kpts(self, start_timestep=0):
        end_timestep = len(self.hands_seq) - start_timestep
        h = 0
        for i, kpts in enumerate(self.kpts_seq):
            black = np.full((self.img_shape[0], self.img_shape[1], 3), 0, dtype=np.int8)

            # draw skeleton
            kpts = np.trunc(kpts) * 1.4
            kpts = kpts.astype(np.int16)
            kpts[:, 0] = self.img_shape[0] - kpts[:, 0]

            for kpt in kpts:
                black = cv2.circle(black, (kpt[0], kpt[1]), 3, (0, 0, 255), -1)
            for adj_kpt in self.adj_kpts:
                start = (kpts[adj_kpt[0]][0], kpts[adj_kpt[0]][1])
                end = (kpts[adj_kpt[1]][0], kpts[adj_kpt[1]][1])
                black = cv2.line(black, start, end, (255, 255, 255), 2)

            if (i >= start_timestep) and (i < end_timestep):
                hands = np.trunc(copy.deepcopy(self.hands_seq[h])) * 0.5
                hands = hands.astype(np.int16)
                hands[:21] = self._match_hand(kpts[4], hands[:21])
                hands[21:] = self._match_hand(kpts[7], hands[21:])

                for j, hand in enumerate(hands):
                    if j > 21:
                        c = (255, 0, 0)
                    else:
                        c = (0, 255, 0)
                    black = cv2.circle(black, (hand[0], hand[1]), 3, c, -1)
                hand_l = hands[:21]
                hand_r = hands[21:]
                for adj in self.adj_hands:
                    black = cv2.line(black, (hand_l[adj[0]][0], hand_l[adj[0]][1]),
                                     (hand_l[adj[1]][0], hand_l[adj[1]][1]), (255, 255, 255,), 2)
                for adj2 in self.adj_hands:
                    black = cv2.line(black, (hand_r[adj2[0]][0], hand_r[adj2[0]][1]),
                                     (hand_r[adj2[1]][0], hand_r[adj2[1]][1]), (255, 255, 255,), 2)
                h += 1

            black = cv2.flip(black, 0)
            tozero = 9 - len(str(i))
            fn = '0' * tozero + str(i) + '.jpg'
            cv2.imwrite(os.path.join(self.save_dir, fn), black)

    def make_video(self):
        imgs = os.listdir(self.save_dir)
        imgs = sorted(imgs)
        frames = []
        for img_file in imgs:
            #print(img_file)
            img = cv2.imread(os.path.join(self.save_dir, img_file))
            h, w , _ = img.shape
            size = (w, h)
            frames.append(img)
        out = cv2.VideoWriter('./test.mp4', cv2.VideoWriter_fourcc(*'DIVX'), 30, size)
        for frame in frames:
            out.write(frame)
        out.release()
if __name__ == '__main__':
    args = parse_args()
    connector = ConnectKptHand(args.kpt_dir, args.hand_dir, args.save_dir)
    connector.read_data()
    connector.draw_kpts(start_timestep=0)
    connector.make_video()



