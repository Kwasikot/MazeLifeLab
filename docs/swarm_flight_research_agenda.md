# Swarm Flight Research Agenda

## 1. Research Direction

MazeLifeLab is pivoting from RRT-centered maze path planning toward swarm-based flying artificial life agents.

The new focus is a population of many small flying agents inspired by:

- bees and hive foraging;
- fruit flies and noisy local search;
- Boids-style flocking;
- Vicsek-style active matter;
- foraging swarms;
- simple social insects and collective behaviour.

The purpose is not to solve a maze with an optimal planner. The purpose is to study how simple local rules can produce visible collective motion, distributed search, environmental memory, and primitive social organization.

---

## 2. Core Research Question

```text
How can simple flying agents, using only local perception and local interaction rules, produce measurable swarm motion, collective search, foraging, and hive-like coordination?
```

Near-term question:

```text
Can many lightweight agents produce stable emergent swarm behaviour before any explicit communication, learning, or planning is added?
```

---

## 3. Agent Model

Agents should be simple flying bodies in a bounded 3D world.

Each agent should have:

- position;
- velocity;
- heading;
- limited perception radius;
- local neighbor sensing;
- simple steering forces;
- optional internal state such as searching, returning home, avoiding threat, or resting.

The early agents should not require neural networks, global maps, or path planners. They should be understandable enough that failures can be diagnosed visually and through metrics.

---

## 4. Local Interaction Rules

The first research layer is local interaction.

Agents should react to nearby agents and the environment using simple forces:

- keep distance from close neighbors;
- align velocity with nearby agents;
- move toward the local center of neighbors;
- wander when no stronger cue exists;
- avoid world boundaries;
- later avoid obstacles, threats, and depleted zones.

These rules should remain modular so later experiments can enable or disable them for ablation.

---

## 5. Swarm Motion

The first visible success condition is coherent swarm motion:

- agents stay near the group without collapsing into one point;
- agents move through 3D space without freezing;
- agents form loose flocks, streams, clusters, or milling patterns;
- the system remains stable across different population sizes.

This stage is a baseline. It does not yet claim intelligence, communication, or collective decision-making.

---

## 6. Obstacle Avoidance

After a basic flying swarm works, agents should respond to obstacles using local perception.

Research questions:

- Can the swarm flow around objects without central planning?
- Does obstacle density fragment the swarm?
- Do local rules create traffic jams, vortices, or bottlenecks?
- Which simple avoidance rules preserve group cohesion?

Obstacle avoidance should be tested as a field or steering force, not as global path planning.

---

## 7. Foraging

Foraging introduces biologically meaningful tasks.

A minimal foraging task should include:

- food or resource sites;
- a home or hive zone;
- agent search behaviour;
- return-to-home behaviour;
- measurable collection events.

The aim is to observe whether local movement, environmental cues, and group dynamics improve discovery and collection over random search.

---

## 8. Hive-Like Coordination

Hive-like behavior should start simple.

Possible mechanisms:

- home attraction;
- resource carrying;
- local recruitment near food;
- rest or recharge near the hive;
- simple role-like states such as scout, forager, carrier, or guard.

The project should avoid overclaiming. A hive-like pattern is only meaningful if metrics show improved search, collection, robustness, or division of labour.

---

## 9. Emergent Collective Search

The central research target is collective search without a central controller.

Useful observable outcomes:

- faster discovery of resources;
- broad spatial coverage;
- reduced redundant search;
- stable return paths;
- adaptive spreading when resources are sparse;
- regrouping when threats appear;
- emergent trails or flow fields.

The swarm should be compared against simple baselines such as independent random flyers and Boids without scent or home attraction.

---

## 10. Simple Social Behavior

Social behavior should be built from minimal local rules.

Early examples:

- attraction to nearby moving agents;
- avoidance of overcrowding;
- recruitment by local signal or motion;
- following recent successful agents;
- dispersal from depleted or crowded zones;
- role switching based on local context.

These should be framed as simple social heuristics, not as human-like cognition.

---

## 11. Experiment Sequence

### EXP-SWARM-001 - 3D Boids Baseline

Purpose:

- establish many small flying agents;
- implement separation, alignment, cohesion, wander, and boundary avoidance;
- measure stability, speed, group cohesion, and coverage.

Baseline comparison:

- independent random flying agents;
- Boids with one steering force removed.

### EXP-SWARM-002 - Obstacle Avoidance Field

Purpose:

- add local obstacle repulsion;
- test whether the swarm can flow around simple 3D obstacles;
- measure collision rate, fragmentation, and traversal coverage.

### EXP-SWARM-003 - Fruit-Fly Search / Random Exploration

Purpose:

- study noisy local search inspired by fruit fly exploration;
- tune random walk, turn noise, persistence, and local cue response;
- compare search coverage against basic Boids.

### EXP-SWARM-004 - Bee-Hive Foraging

Purpose:

- add hive/home zone and resource sites;
- measure resource discovery, return success, and collection rate;
- compare independent foragers against socially influenced foragers.

### EXP-SWARM-005 - Scent / Pheromone Field

Purpose:

- add environmental scent or pheromone fields;
- test whether decaying cues improve foraging and collective search;
- measure signal usage, trail formation, redundancy, and resource return.

### EXP-SWARM-006 - Threat / Predator Response

Purpose:

- introduce moving or static threats;
- measure avoidance, regrouping, swarm splitting, and survival;
- test whether local threat signals improve collective robustness.

### EXP-SWARM-007 - Role Differentiation

Purpose:

- add simple role-like behavioral states;
- test scout, forager, carrier, guard, or rest states;
- measure whether role differentiation improves efficiency over homogeneous agents.

---

## 12. Algorithm Proposal: Scented Active Boids

`Scented Active Boids` is the proposed initial algorithm family for swarm-flight experiments.

It begins as Boids-style active matter with random exploration and later grows into foraging, scent, memory, and threat response.

### Initial Steering Forces

```text
separation
alignment
cohesion
random_wander
boundary_avoidance
```

These forces are enough for `EXP-SWARM-001`.

### Future Steering Forces

```text
obstacle_repulsion
odor_gradient
home_attraction
pheromone_field
memory_field
threat_avoidance
```

These should be added only when their experiment requires them.

### Sketch

```text
for each agent:
    neighbors = sense_agents_within_radius()
    cues = sense_environment_within_radius()

    steering =
        separation(neighbors)
      + alignment(neighbors)
      + cohesion(neighbors)
      + random_wander()
      + boundary_avoidance()

    if obstacle experiment enabled:
        steering += obstacle_repulsion(cues)

    if foraging experiment enabled:
        steering += odor_gradient(cues)
        steering += home_attraction(agent_state)

    if scent experiment enabled:
        steering += pheromone_field(cues)
        deposit_or_decay_scent(agent_state)

    if threat experiment enabled:
        steering += threat_avoidance(cues)

    velocity = limit_speed(velocity + steering)
    position = position + velocity * dt
```

---

## 13. Candidate Metrics

Minimum metrics for `EXP-SWARM-001`:

```text
episode_id
seed
agent_count
mean_speed
mean_neighbor_distance
cohesion_index
separation_violations
boundary_hits
coverage_volume_percent
termination_reason
```

Future metrics:

```text
food_discovered
food_returned
time_to_first_food
foraging_efficiency
pheromone_deposits
trail_stability
threat_contacts
survival_rate
role_distribution
role_switches
```

---

## 14. Research Discipline

RRT work remains historically useful as a planning baseline and implementation archive, but it is no longer the main research center.

The new center is:

```text
many simple flying agents
local rules
visible emergent swarm motion
measurable foraging and collective search
```

Implementation should proceed from the simplest measurable swarm baseline before adding pheromones, predators, roles, learning, or cognition-like mechanisms.
