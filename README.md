# HarryPotterMagic
A cool project to spread STEM interest. Use a Harry Potter Wand, the IR Camera in an Xbox Kinect, and my AI model to cast spells and animate a robot.

---

SpellNet contains the Python scripts needed to generate the AI model. As this is a relatively quick-and-dirty project, relevant paths are hardcoded. Important-to-change paths are hardcoded:
1. The Data Path in the SpellNet/SpellNet.py script
2. The Model Path in SpellAI.cs

I've uploaded my WandShots (training data) here: [https://1drv.ms/u/s!Amfq1rYlnL0LhKR3GZIQ-pJv0SSWSw?e=nbHxpe](https://1drv.ms/u/s!Amfq1rYlnL0LhKR3GZIQ-pJv0SSWSw?e=nbHxpe)

To anyone looking to replicate this project, you should be able to open and compile this project using Visual Studio 2019. At the time of writing, I unfortunately do not remember all this project's dependencies, although I believe the Kinect SDK is a must-have. This tutorial looks pretty solid: [https://www.packt.com/getting-started-kinect-windows-sdk-programming/](https://www.packt.com/getting-started-kinect-windows-sdk-programming/)
