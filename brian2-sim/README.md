# Brian2 wetware sidecar

This folder is the stand-in for living wetware. Unity does not talk to Brian2 at runtime. You run this script, it writes files, and `ReplayFileWetware` plays those files back inside the agent.

## Install

You need Python 3.9 or newer.

```bash
cd brian2-sim
python3 -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
```

On Windows, activate with `.venv\Scripts\activate`.

## Run

```bash
python simulate_stdp.py
```

It writes:

- `output/brian2_replay.csv` with columns `t,n0,n1,n2,n3,s0,s1,s2,s3`
- `output/brian2_weights.json` with the STDP weights over time

Copy the CSV to `Assets/DreamMachine/Resources/Wetware/brian2_replay.csv` (replace the sample) and press Play again. The status line reports how many rows loaded.

The CSV and `output/brian2_weights.json` already in this repo were produced by this script: 400 ms, 4 neurons, 17 spikes, and STDP weights moving from 0.55 into the mid 0.7s. If you do not have Python development headers, the script uses Brian2's numpy code generator and skips Cython.

`n0`–`n3` are normalized membrane readouts. `s0`–`s3` are spikes (1 or 0). The Unity replay steps one row at a time. It does not learn on top of the recording. The live babies still use the small Hebbian model for their own reward signal.

## What this is not

It is not a FinalSpark organoid, and it is not connected to a lab. When a real recording exists, keep the same CSV shape (or add another `IWetware` implementation) and the rest of the agent stays the same.
