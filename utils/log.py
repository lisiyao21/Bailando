# This source code is licensed under the MIT license found in the
# LICENSE file in the root directory of this open-source project.


""" Define the Logger class to print log"""
import os
import sys
import logging
from datetime import datetime


class Logger:
    def __init__(self, args, output_dir):

        log = logging.getLogger(output_dir)
        if not log.handlers:
            log.setLevel(logging.DEBUG)
            # if not os.path.exists(output_dir):
            #     os.mkdir(args.data.output_dir)
            fh = logging.FileHandler(os.path.join(output_dir,'log.txt'))
            fh.setLevel(logging.INFO)
            ch = ProgressHandler()
            ch.setLevel(logging.DEBUG)
            formatter = logging.Formatter(fmt='%(asctime)s %(message)s', datefmt='%m/%d/%Y %I:%M:%S')
            fh.setFormatter(formatter)
            ch.setFormatter(formatter)
            log.addHandler(fh)
            log.addHandler(ch)
        self.log = log
        # setup TensorBoard
        # if args.tensorboard:
        #     from tensorboardX import SummaryWriter
        #     self.writer = SummaryWriter(log_dir=args.output_dir)
        # else:
        self.writer = None
        self.log_per_updates = args.log_per_updates

    def set_progress(self, epoch, total):
        self.log.info(f'Epoch: {epoch}')
        self.epoch = epoch
        self.i = 0
        self.total = total
        self.start = datetime.now()

    def update(self, stats):
        self.i += 1
        if self.i % self.log_per_updates == 0:
            remaining = str((datetime.now() - self.start) / self.i * (self.total - self.i))
            remaining = remaining.split('.')[0]
            updates = stats.pop('updates')
            stats_str = ' '.join(f'{key}[{val:.8f}]' for key, val in stats.items())
            
            self.log.info(f'> epoch [{self.epoch}] updates[{updates}] {stats_str} eta[{remaining}]')
            
            if self.writer:
                for key, val in stats.items():
                    self.writer.add_scalar(f'train/{key}', val, updates)
        if self.i == self.total:
            self.log.debug('\n')
            self.log.debug(f'elapsed time: {str(datetime.now() - self.start).split(".")[0]}')

    def log_eval(self, stats, metrics_group=None):
        stats_str = ' '.join(f'{key}: {val:.8f}' for key, val in stats.items())
        self.log.info(f'valid {stats_str}')
        if self.writer:
            for key, val in stats.items():
                self.writer.add_scalar(f'valid/{key}', val, self.epoch)
        # for mode, metrics in metrics_group.items():
        #     self.log.info(f'evaluation scores ({mode}):')
        #     for key, (val, _) in metrics.items():
        #         self.log.info(f'\t{key} {val:.4f}')
        # if self.writer and metrics_group is not None:
        #     for key, val in stats.items():
        #         self.writer.add_scalar(f'valid/{key}', val, self.epoch)
        #     for key in list(metrics_group.values())[0]:
        #         group = {}
        #         for mode, metrics in metrics_group.items():
        #             group[mode] = metrics[key][0]
        #         self.writer.add_scalars(f'valid/{key}', group, self.epoch)

    def __call__(self, msg):
        self.log.info(msg)


class ProgressHandler(logging.Handler):
    def __init__(self, level=logging.NOTSET):
        super().__init__(level)

    def emit(self, record):
        log_entry = self.format(record)
        if record.message.startswith('> '):
            sys.stdout.write('{}\r'.format(log_entry.rstrip()))
            sys.stdout.flush()
        else:
            sys.stdout.write('{}\n'.format(log_entry))


