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
|        List Contains |    344 |             0,9 |          3596,3 |           960,5 |         1800,1 |
|        ChTr Contains |    344 |             0,8 |           316,1 |             4,9 |           29,4 |
|----------------------|--------|-----------------|-----------------|-----------------|----------------|
|      List StartsWith |     10 |         65911,6 |         76096,2 |         71021,2 |        71256,8 |
|    ChTr Find(prefix) |     10 |            90,7 |          4131,7 |           697,2 |          635,3 |
|----------------------|--------|-----------------|-----------------|-----------------|----------------|
|        List EndsWith |      5 |        115646,6 |        130953,1 |        122747,1 |       120506,2 |
|    ChTr Find(suffix) |      5 |         11637,6 |         17527,5 |         13864,9 |        14285,5 |
|----------------------|--------|-----------------|-----------------|-----------------|----------------|
|           List Regex |      3 |         76181,9 |         80872,3 |         79273,4 |        80765,9 |
|   ChTr Find(pattern) |      3 |         31072,5 |         33852,6 |         32842,9 |        33603,7 |
|----------------------|--------|-----------------|-----------------|-----------------|----------------|