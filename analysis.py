from convokit import Corpus, download
import pandas as pd

corpus = Corpus(filename=download("movie-corpus"))

corpus.print_summary_stats()

splits = {'train': 'data/train-00000-of-00001.parquet', 'validation': 'data/validation-00000-of-00001.parquet'}
df = pd.read_parquet("hf://datasets/lparkourer10/twitch_chat/" + splits["train"])

print(df.shape)
print(df.head())
