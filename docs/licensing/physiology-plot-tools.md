# M1 plotting tools (development only)

Matplotlib 3.10.7 and its dependencies were installed from Python package-index wheels into
ignored artifacts/physiology/plot-venv for engineering charts. They are not game assets, Unity dependencies,
or included in player builds. Resolved versions are pinned in scripts/requirements-physiology-plots.txt.
Original license texts remain in each distribution's .dist-info/licenses/metadata in that environment.
Matplotlib uses its bundled project license; NumPy metadata lists BSD/MIT/0BSD/Zlib/CC0 components;
FontTools, pyparsing and six MIT; packaging Apache-2.0 OR BSD-2-Clause; Pillow MIT-CMU.
Other transitive license texts are preserved in their installed distributions. No paid asset or account required.

Generated plots use only project simulation data and Matplotlib's bundled fonts. Before distributing
Python tooling itself, carry its original notices; the Unity release has no dependency on this environment.
