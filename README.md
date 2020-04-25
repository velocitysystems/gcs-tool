# gcs-tool
A .NET Core console application to transcribe audio using the Google Speech-to-Text API.

**Features**
- Supports long-running transcription

**Supported Platforms**
- Windows
- Linux
- Unix, macOS

**Usage**

|Flag|Description|Required|
|---|---|---|
|`-c "credentials.json"`|The path to the "credentials.json" file|True|
|`-a "audio.mp3"`|The path to the audio file to transcribe|True|

**Requirements**

Enable the Google Speech-to-Text API as described [here](https://cloud.google.com/speech-to-text/docs).

1. Go to the [Google API Console](https://console.developers.google.com/).
2. Select a project.
3. In the sidebar on the left, expand **APIs & auth** and select **APIs**.
4. In the displayed list of available APIs, click the Speech-to-Text link and click **Enable API**.


**Disclaimer**

*The material embodied in this software is provided to you "as-is" and without warranty of any kind, express, implied or otherwise, including without limitation, any warranty of fitness for a particular purpose. In no event shall the author be liable to you or anyone else for any direct, special, incidental, indirect or consequential damages of any kind, or any damages whatsoever, including without limitation, loss of profit, loss of use, savings or revenue, or the claims of third parties, whether or not the author has been advised of the possibility of such loss, however caused and on any theory of liability, arising out of or in connection with the possession, use or performance of this software.*
