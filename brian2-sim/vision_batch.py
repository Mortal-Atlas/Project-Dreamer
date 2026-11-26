"""
Optional spiking vision. Off by default on the phone.

Reads a batch of raw grayscale frames (uint8, 16x16 or 32x32) and runs one
Brian2 STDP pass over the whole batch. The Unity client does not call this
every tick. It writes a batch, runs this once, and loads the spike file.
"""

import json
import sys
from pathlib import Path

import numpy as np
from brian2 import ms, NeuronGroup, SpikeMonitor, prefs, start_scope, run


def main():
    prefs.codegen.target = "numpy"
    if len(sys.argv) < 3:
        print("usage: vision_batch.py frames.u8 out.json [width]")
        return 1
    width = int(sys.argv[3]) if len(sys.argv) > 3 else 16
    raw = Path(sys.argv[1]).read_bytes()
    frame = width * width
    count = len(raw) // frame
    frames = np.frombuffer(raw[: count * frame], dtype=np.uint8).reshape(count, frame) / 255.0
    rates = frames.mean(axis=0)
    start_scope()
    cells = NeuronGroup(frame, "dv/dt = (rates_i - v) / (20*ms) : 1", threshold="v>0.65", reset="v=0", method="exact")
    cells.namespace["rates"] = rates
    cells.v = 0
    spikes = SpikeMonitor(cells)
    run(50 * ms)
    counts = [0] * frame
    for index in spikes.i:
        counts[int(index)] += 1
    Path(sys.argv[2]).write_text(json.dumps({"width": width, "frames": count, "spikeCounts": counts}), encoding="utf-8")
    print("frames", count, "spikes", int(sum(counts)))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
