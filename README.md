# BlueHeron.Collections.CharTrie

## Introduction

The CharTrie is a combination of a Trie and a directed acyclic word graph (DAWG) that results in a very compact representation of a list of words that allows for very fast search operations.
The available Find function accepts a PatternMatch object that enables the standards searches equivalent to '==', 'StartsWith' and 'EndsWith', but also more complex patterns, e.g.: '2nd letter is 'A' AND 4th letter is 'O' OR 'Ö'.
The Tests project's functions demonstrate the possibilities in detail.
A CharTrieFactory is also available that helps in creating (new or from a dictionary / word list) and (de-)serializing CharTrie objects.

## Usage

<! TODO -->

## Benchmark ([history](BENCHMARKS.md))

### TestContext Messages:
|---------|------------|--------------|
| Object  |    # Nodes |         Size |
|---------|------------|--------------|
|   List  |     343075 |   20482984 B |
|---------|------------|--------------| <! Using diagnostic tools snapshot
|   ChTr  |     196782 |    4260336 B |
|---------|------------|--------------|

|----------------------|--------|-----------------|-----------------|-----------------|----------------|
|            Operation | # Runs | Minimum (µsec.) | Maximum (µsec.) | Average (µsec.) | Median (µsec.) |
|----------------------|--------|-----------------|-----------------|-----------------|----------------|
|        List Contains |    344 |             1,3 |          2434,4 |           844,4 |         1217,2 |
|        ChTr Contains |    344 |             0,8 |           294,8 |             4,2 |           14,0 |
|----------------------|--------|-----------------|-----------------|-----------------|----------------|
|      List StartsWith |     10 |         64292,9 |         73024,9 |         68762,5 |        68645,8 |
|    ChTr Find(prefix) |     10 |            84,2 |          4180,3 |           694,5 |          661,9 |
|----------------------|--------|-----------------|-----------------|-----------------|----------------|
|        List EndsWith |      5 |        111555,0 |        122478,6 |        116270,3 |       117981,6 |
|    ChTr Find(suffix) |      5 |         11422,1 |         16658,1 |         13036,7 |        13013,3 |
|----------------------|--------|-----------------|-----------------|-----------------|----------------|
|           List Regex |      3 |         62164,3 |         76388,0 |         69701,1 |        62164,3 |
|   ChTr Find(pattern) |      3 |         22418,2 |         25854,4 |         24117,1 |        24078,6 |
|----------------------|--------|-----------------|-----------------|-----------------|----------------|