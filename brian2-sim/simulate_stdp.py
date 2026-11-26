"""
Brian2 wetware stand-in for the Dream Machine.

Simulates a tiny spiking network with spike-timing-dependent plasticity
and writes the CSV that Unity's ReplayFileWetware already knows how to load.

This is not a brain, and it is not FinalSpark. It is a file the Unity
IWetware replay implementation can step through until a real recording exists.
"""

import json
from pathlib import Path

from brian2 import Hz, ms, mV, NeuronGroup, PoissonGroup, Synapses, SpikeMonitor, StateMonitor, prefs, start_scope, run

# Numpy codegen runs without a C compiler. Cython is faster if Python headers are installed.
prefs.codegen.target = "numpy"


def main():
    start_scope()
    n_inputs = 4
    tau = 10 * ms
    vt = -54 * mV
    vr = -60 * mV
    el = -60 * mV
    taupre = 20 * ms
    taupost = 20 * ms
    gmax = 0.8
    dApre = 0.05
    dApost = -0.055

    sources = PoissonGroup(n_inputs, rates=[8 * Hz, 14 * Hz, 6 * Hz, 18 * Hz])
    cells = NeuronGroup(
        n_inputs,
        """
        dv/dt = (el - v) / tau : volt
        """,
        threshold="v > vt",
        reset="v = vr",
        method="exact",
    )
    cells.v = vr
    synapses = Synapses(
        sources,
        cells,
        """
        w : 1
        dApre/dt = -Apre / taupre : 1 (event-driven)
        dApost/dt = -Apost / taupost : 1 (event-driven)
        """,
        on_pre="""
        v_post += w * 18 * mV
        Apre += dApre
        w = clip(w + Apost, 0, gmax)
        """,
        on_post="""
        Apost += dApost
        w = clip(w + Apre, 0, gmax)
        """,
    )
    synapses.connect(j="i")
    synapses.w = 0.55

    spikes = SpikeMonitor(cells)
    voltage = StateMonitor(cells, "v", record=True, dt=5 * ms)
    weights = StateMonitor(synapses, "w", record=True, dt=5 * ms)
    run(400 * ms)

    out_dir = Path(__file__).resolve().parent / "output"
    out_dir.mkdir(exist_ok=True)
    csv_path = out_dir / "brian2_replay.csv"
    times = voltage.t[:]
    with csv_path.open("w", encoding="utf-8") as handle:
        handle.write("t,n0,n1,n2,n3,s0,s1,s2,s3\n")
        spike_times = [set() for _ in range(n_inputs)]
        for t, i in zip(spikes.t, spikes.i):
            # Bin spikes onto the voltage sample grid so each row lines up.
            index = int(round(float(t / ms) / 5.0))
            if 0 <= i < n_inputs:
                spike_times[i].add(index)
        for row, t in enumerate(times):
            rates = []
            fired = []
            for i in range(n_inputs):
                # Map voltage into a 0..1 rate-like readout for the Unity replay.
                volts = float(voltage.v[i, row] / mV)
                rates.append((volts - (-70.0)) / 30.0)
                fired.append(1 if row in spike_times[i] else 0)
            handle.write(
                "{:.4f},{:.5f},{:.5f},{:.5f},{:.5f},{},{},{},{}\n".format(
                    float(t / ms) / 1000.0,
                    rates[0],
                    rates[1],
                    rates[2],
                    rates[3],
                    fired[0],
                    fired[1],
                    fired[2],
                    fired[3],
                )
            )

    weight_path = out_dir / "brian2_weights.json"
    # Last weight of each one-to-one synapse, plus a short history.
    history = []
    for row in range(0, weights.w.shape[1], 4):
        history.append([float(weights.w[i, row]) for i in range(n_inputs)])
    weight_path.write_text(
        json.dumps(
            {
                "kind": "brian2-stdp",
                "neurons": n_inputs,
                "finalWeights": [float(weights.w[i, -1]) for i in range(n_inputs)],
                "history": history,
            },
            indent=2,
        ),
        encoding="utf-8",
    )
    print("Wrote", csv_path)
    print("Wrote", weight_path)


if __name__ == "__main__":
    main()
