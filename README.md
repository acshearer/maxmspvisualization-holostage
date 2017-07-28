# maxmspvisualization-holostage

* Tested with : Unity 5.6.2f1 & Visual Studio Community 2017 V. 15.2 (26430.16)
* To compile for Hololens
    * Open cloned folder as Unity Project
    * File->Open Scene->Assets/Scenes/maxmspvisualizationUWP.unity
    * File->Build Settings->![Copy settings from here](http://i.imgur.com/ZlyN7W4.png)
    * Click Build and target an empty folder (App)
    * Open App/maxmspvisualization.sln
    * Target Hololens in Visual Studio, build/deploy
    * Make sure configuration is set to Master or the program will run very slow
* To show jit.matrix from max
    * Send a jit.matrix with the built-in jit.net.send object with the correct IP and port (default 7474)
    * Data will appear in front of starting position of program

* To compile for native (OSX/Windows)
    * Delete Assets/HoloToolkit/* and Assets/Vuforia/*
    * Other errors may appear, delete conflicting files.
    * Target standalone in File->Build Settings
    * Run and send data same as Hololens(UWP) application on your computer