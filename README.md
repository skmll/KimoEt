# KimoEt
Eternal Card Game Drafting Tool

Tool for the Eternal Card Game's Draft Mode that lets you have an overlay over the game's window that shows ratings for each card while drafting.
This is especially helpful for beginners.
If you are goint to use it, beware that the ratings are only a representation of a card's power in the void. You should adjust your picks according to what you have already picked. This is more and more important as you go through your picks.

The source of the ratings is this document made by TDC's Eternal team (please correct me if I am wrong):
https://docs.google.com/spreadsheets/d/1NH1i_nfPKhXO53uKYgJYICrTx_XSqDC88b2I3e0vsc0

This was done out of a personal frustration I had with needing to ALT+TAB for the spreadsheet, CTRL+F for searching, writting the name, looking for the highlighted card.

To use it, download latest version from releases tab:
https://github.com/skmll/KimoEt/releases

Or use this link: (might not be updated for future releases)
https://github.com/skmll/KimoEt/releases/download/v1.0/KimoEt_v1.0.zip

After that, uncompress it, and open KimoEt folder. Then just double click on KimoEt.exe to star the program.
It should be pretty easy after that. Go into a draft, and click the "play" button. You should see something like this:
![alt text](https://i.imgur.com/5nAm9FA.jpg)

FEATURES:
- See the ratings (0-5) for each card in the current draft screen
- Colors for each rating range (Default goes from red to yellow to green, to a cyanish to white color for 4+ rated cards)
- Click on the name of a card, to see individual ratings and comments the reviewers might have added
- If some card is wrong (it might happen on rare ocasions), there is a search icon next to the name, you can use it to search for the correct card by its name (case insensitve but other than that it has to match exactly)
- Three modes of ratings' colors (User, Default, NONE)
- Change the colors for each rating range (User MODE)
- Refresh
- Play/Pause
- Auto hides when putting Eternal on Background (works most of the time, I haven't had time to address this yet. If it doesnt work the first time, do some more alt tabs :P)
- Auto moves the overlay window when Eternal window moves
- Auto brings Eternal window to front when hitting the "play" button
- All app windows are draggable, so you can move them around

KNOWN LIMITATIONS:
- Only works for Windows
- Only works in 1920x1080 resolution (Eternal game setting)
- Only works in Windowed Mode (Eternal game setting)
- Sometimes the auto hiding when Eternal is not in the foreground, might not work

DISCLAIMER:

I did this tool on my free time, only tested on my own personal computer. As such, I cannot promise this will work on your computer nor am I responsible in any way for anything that happens to your PC after using the tool.
I am a software developer, but I had no previous experience with any tool I used in this app (Tesseract, JSONConverter) and I don't fully understand how they operate.
It is possible that I could have also implemented something that I am not fully aware of how it works.
I personaly have been using it almost every day. I love it and I dont have any issue with it.

I am not proud of the code quality, since I focused on speed of development, giving the little time I had to do it daily.
I would appreciate and encourage if someone from c# wpf world would do some pull requests to help me improve that.
I am also open to ideas and features you guys would like me to add on future releases.
If you find any bugs, please open an issue or contact me.

Big thanks to the TDC team for providing me and all of us with the great tool that is the spreadsheet. It enabled me to do this, and all of us to have an idea of the cards' power when we dont have much time to play with everything.

Contact me:

Eternal discord -> zkeme

In game name -> sKeeme+0851
