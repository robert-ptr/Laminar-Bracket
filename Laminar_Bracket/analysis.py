from convokit import Corpus, download
import kagglehub
from kagglehub import KaggleDatasetAdapter
import pandas as pd
import pickle
import numpy as np
import os

corpus = Corpus(filename=download("movie-corpus"))

corpus.print_summary_stats()

splits = {'train': 'data/train-00000-of-00001.parquet', 'validation': 'data/validation-00000-of-00001.parquet'}
df = pd.read_parquet("hf://datasets/lparkourer10/twitch_chat/" + splits["train"])

print(df.shape)
print(df.head())

path = kagglehub.dataset_download("sammahoney/esa-anomaly-dataset")

print(f"Dataset descărcat la: {path}")

mission = 1
channel_id = 1
channel_file = os.path.join(path, f'ESA-Mission{mission}', f'ESA-Mission{mission}', 'channels', f'channel_{channel_id}', f'channel_{channel_id}')

with open(channel_file, 'rb') as f:
    data = pickle.load(f)

df = pd.DataFrame(data)

print(df.shape)
print(df.head())

path = kagglehub.dataset_download("sfu-summit/starcraft-ii-replay-analysis")

#print("Path to dataset files:", f"{path}/starcraft.csv")
files = os.listdir(path)
print("Fișiere disponibile în folder:")
for f in files:
    print(f"- {f}")
df = pd.read_csv(f"{path}/starcraft.csv")

print(df.shape)
print(df.head())
