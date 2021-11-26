I've had some ideas on how to improve it further, making fine-tuning unnecessary and the CPU time saved being optimal.

Idea would be:
- When a conveyor lookup fails record that item to be "missing" on the network permanently.
- Listen on all inventory changes and if that item changes amount anywhere on the same network, then remove it from the "missing" set.
- 
It would basically optimize out the whole Keen algorithm as long as an item is missing.

Drawback is it needs a bit more memory and maintaining a set, but that would be minimal.

The same could be done for gases (H2/O2). But Keen improved that, so I'm not sure it would help. Needs profiling first. 