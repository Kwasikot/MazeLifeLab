# BallPark

  + - - - + - + - -
  + - + - + Kwasikot  <br>
```
                            )            (
                           /(   (\___/)  )\
                          ( #)  \ ('')| ( #
                           ||___c\  > '__||
                           ||**** ),_/ **'|
                     .__   |'* ___| |___*'|
                      \_\  |' (    ~   ,)'|
                       ((  |' /(.  '  .)\ |
                        \\_|_/ <_ _____> \______________
                         /   '-, \   / ,-'      ______  \
                b'ger   /      (//   \\)     __/     /   \
                                            './_____/
```              
# The initial goal of the project:
Make a robots that can learn how to go through the maze.
I'm using machine learning approach i.e. reinforcement learning and imitation learning to achieve this goal.
In the first phases of the current project in 2020 than coronavirus was a popular thing I began to work on RRT trees.
First idea was to create car-like robots that can park in particular slots. At that time this project goal was inspired by my job at that time in the Luxoft company.
I discovered and implement Rapid Exploring Random Trees. But they  was really stone-like in the machine learning sense.
So I started to find ways how I can extend the rrt idea. I wrote a little hack that spread the reward across the RRT in the maze.
When I start to train agents to get a reward like checkpoints in the race. This gave me the initial results.
But the agents were so dumb. They frequently hit the walls. I dont realise that was the failure related to my not outstanding experience in machine learning.
I use reinforcement learning instead of imitation learning. 
For training the bots i'm using a machine learning environment provided by mlagents developers.
In the end of a day I decided to delete my old repository and start the project from scratch.
Just rewrite the code, fix bugs implemented in the foundation. 
It helps also to set new goals for the project. Now i'm fan of the artificial life science.
According to some experts this is the true path to achieve AGI. 

# Current goal (22/07/2023)
Idea is to localize and simpilfy the RRT building procces in terms of exploration and number of operations. Get rid of slow runge-kutta calculations. And give agents ability to send sound or light signals for communication using some sort of a language.
So this would be an imitation of social interaction when agents naturally find the way to communicate with each other and how to find a way through the maze.

![image](https://github.com/Quasikot/BallPark/blob/main/images/Screenshot%202023-07-27%20112045.png?raw=true)
  
Литература:
* Nonlinear Dynamics And Chaos With Applications To Physics, Biology, Chemistry, And Engineering by Steven H. Strogatz
* Rapidly-Exploring Random Trees: Progress and Prospects
* Unity Machine Learning Agents
* Turgut, A. E., Çelikkanat, H., Gökçe, F., & Şahin, E. (2008). Self-organized flocking in mobile robot swarms. Swarm Intelligence, 2, 97–120.
* Trianni, V., & Dorigo, M. (2006). Self-organisation and communication in groups of simulated and physical robots. Biological Cybernetics, 95, 213–231.
* Baldassarre, G., Trianni, V., Bonani, M., Mondada, F., Dorigo, M., & Nolfi, S. (2007). Self-organized coordinated motion in groups of physically connected robots
* [Self-Organization and Artificial Life](https://direct.mit.edu/artl/article/26/3/391/93243/Self-Organization-and-Artificial-Life)
  
  
# 🚗 Maze AI Project

## 🎯 Project Overview
This project is a revival of my old **BallPark** idea, but this time powered by **AI and ML-Agents**.  
The goal is to explore algorithms of **path planning, reinforcement learning, and artificial life** in maze environments.  

It combines:  
- 🌀 Procedural maze generation (`MazeGen.cs`)  
- 🌳 Path planning via **Rapidly-Exploring Random Trees (RRT)** (`Rrt.cs`)  
- 🤖 Intelligent agents that can **scan, learn, and communicate** (`AgentControl.cs`)  
- 🧠 Integration with **Unity ML-Agents** (reinforcement learning, imitation learning)  

---

## 🚀 MVP Plan

### Phase 1. Core mechanics
- [x] Simple car/agent movement in Unity  
- [x] Basic procedural maze generation  
- [ ] Manual control mode (keyboard/joystick)  
- [ ] Visualize walls and trajectories  

### Phase 2. Path planner
- [ ] Implement RRT planner (without Runge-Kutta integration at first)  
- [ ] Visualize search tree in real-time  
- [ ] Switch modes: **Manual ↔ RRT**  

### Phase 3. AI Integration
- [ ] Connect **Unity ML-Agents** (Python backend + Unity simulation)  
- [ ] Define basic reward (e.g. reaching checkpoints, avoiding walls)  
- [ ] Train an agent to navigate the maze  

### Phase 4. Multi-agent experiments
- [ ] Run multiple agents simultaneously  
- [ ] Add simple communication (light / sound signals)  
- [ ] Compare strategies: **RRT vs RL vs emergent swarm behavior**  

---

## 📋 Requirements
We can use inspiration from biology. Such as [TAG: A Decentralized Framework for Multi-Agent Hierarchical Reinforcement
Learning](https://arxiv.org/pdf/2502.15425)

In this particular work i don't need any car-like system. This is not a goal of the project i mean to build an autopilot.
The main goal of this project is to simulate collective intelligence, hierarchical agent systems that solves some survival or maybe path planing tasks.

### Functional
- Agent can move in a 3D maze  
- Maze is procedurally generated  
- Path planning via RRT  
- RL training with ML-Agents  
- Switchable modes: Manual / RRT / RL  
- Multi-agent support with basic communication  

### Non-functional
- Runs in real-time on an average laptop  
- Modular code structure (Agents / Maze / Planners)  
- Documented workflow in README  
- Simple visualization with Unity Gizmos and Debug.DrawLine  

---

## 🛠 Tech Stack
- **Unity 2022+**  
- **C#** (agents, maze, planners)  
- **Python 3.10+** (training logic with PyTorch via ML-Agents)  
- **ML-Agents Toolkit**  

---

## 📚 References
- Strogatz, *Nonlinear Dynamics and Chaos*  
- Rapidly-Exploring Random Trees: Progress and Prospects  
- [Unity ML-Agents](https://github.com/Unity-Technologies/ml-agents)  
- Turgut et al., *Self-organized flocking in mobile robot swarms*  
- Trianni & Dorigo, *Self-organisation and communication in groups of simulated robots*
- [Continuous Thought Machines](https://arxiv.org/abs/2505.05522)


---

## 🌌 Vision
The long-term vision is to create a **virtual lab of artificial life**:  
- Agents that evolve navigation strategies  
- Communication and cooperation in swarms  
- A bridge between **AI, robotics, and computational physics**  
 
