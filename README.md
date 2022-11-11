# TextHighlighter
A sample app that uses Microsoft.Cognitive.Speech to analyse a wav file and show the text with a highlight on the words currently pronounced

to be able to run this app you need an access key to an instance of azure speech service
(5 hours analysis free per month)

You can create one by 
- going to portal.azure.com and logging in
- click on "create a resource"
- search for "speech", add the service
- on the overview tab you see "Manage keys: Click here to manage keys"
- on this screen you will find 2 keys, one of which you need to add to the registry under "SPEECH_KEY"

Now you need to add two keys to the registry
- SPEECH_KEY with the value that you found in the "manage keys" screen.
- SPEECH_REGION with the value of the region where you created the service, for instance 'westeurope'
Link 1: shows that you can add the registry key by typing "setx SPEECH_KEY your-key" in console.
The keys will be created in the registry under HKEY_CURRENT_USER\ENVIRONMENT

Afterwards restart console / Visual studio

You are now ready to run the app.

A sample audio file is included in Assets\Sounds directory.

Link 1: https://learn.microsoft.com/en-us/azure/cognitive-services/speech-service/get-started-speech-to-text?tabs=windows%2Cterminal&pivots=programming-language-csharp



