
***SETUP***

Ahoy artists...

This document shows you how to finish setting up fast platform switching in each art project. These instructions assumes that you already ran the AltspaceVR->Platform->Set Up Platform Switching Folder menu option in the corresponding Unity project. If you have not, please do that right now before following the instructions below.

1. Switch to the Windows platform in the art project (navigate to File->Build Settings; click on the PC/Mac platform; click "Switch Platform"). Close Unity, and any instances of MonoDevelop or Visual Studio that are being used to edit scripts in that project.
2. Go to your Unity art project's folder. It should contain folders like Assets and ProjectSettings.
3. Open the LibraryCache folder that exists in your Unity project directory.
4. Double click FastPlatformSetupWindows.bat.

And that's it. 

***INSTRUCTIONS POST-SETUP***

You can now start Unity up again. If you want to switch platforms from here on out, use AltspaceVR->Platform->Switch to <Platform>, where <Platform> is Windows or Android. Note that switching to Android from Windows for the first time will be slow but will be much faster after the initial platform switch to Android has been done. 

Please don't use Unity build setting's platform switching while fast platform switching is being used. If you want to return to that type of (slow as molasses) platform switching then close Unity, delete the Library and LibraryCache folders, and start Unity again.

If you are building asset bundles for Mac, switch to Windows first using the AltspaceVR->Platform menu.

VERY IMPORTANT: Make sure that Perforce ignores the LibraryCache folder as well! That's where the Library files will be located.