---
name: great-circle-distance
description: Calculates the great-circle (Haversine) distance between two latitude/longitude coordinates. Use when computing aviation leg distance, navigation ranges, or FMS route segment lengths.
---

# Great-Circle Distance Skill

This skill computes the shortest surface distance between two geographic coordinates using the Haversine formula.

Use this whenever:
- Calculating route leg distance in an FMS
- Displaying nautical mile distance on an ND
- Validating waypoint-to-waypoint separation
- Performing accurate Earth-surface navigation math

Do NOT use flat/planar math unless both points are already projected and local-scale error is acceptable.

---

## Required Inputs
- lat1 (decimal degrees)
- lon1 (decimal degrees)
- lat2 (decimal degrees)
- lon2 (decimal degrees)

All coordinates must be in **decimal degrees**.

---

## Procedure

1. Convert all degrees to radians:

```
radians = degrees × (π / 180)
```

2. Compute differences:

```
Δlat = lat2 − lat1
Δlon = lon2 − lon1
```

3. Compute intermediate value:

```
a = sin²(Δlat / 2) +
    cos(lat1) × cos(lat2) × sin²(Δlon / 2)
```

4. Compute central angle:

```
c = 2 × atan2(√a, √(1 − a))
```

5. Multiply by Earth radius:

```
distance = R × c
```

---

## Earth Radius Constants

Choose based on required output unit:

- 6,371,000 → meters
- 6,371 → kilometers
- 3,440.065 → nautical miles (preferred for aviation)

---

## Output
Returns the great-circle distance between the two coordinates in the selected unit.

---

## Implementation Notes

- Always store coordinates internally as decimal degrees.
- Always convert to radians before trig functions.
- Prefer nautical miles for aviation contexts.
- This method assumes a spherical Earth (sufficient for training simulations and standard nav computations).

