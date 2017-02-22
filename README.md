# Support Game Pipe
[![paypal](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=FCH86TKH6V35A)

For now, you can support Game Pipe via Paypal donation (note: I am not a non-profit, your donation is used to support and encourage future development). In the near future, Game Pipe will be purchasable on Amazon, Itch.io, and Windows Store for $5.

Sadly, __Game Pipe will probably never be available on Steam__, because of Valve's backwards attitudes towards software (even with the upcoming Steam-Direct changes, they have indicated they will not allow utility software like Game Pipe). However, if by some miracle Valve changes their policies, I will do my best to provide Steam key's to anyone who has donated via paypal or purchased Game Pipe on other platforms.

# About Game Pipe
Game Pipe is an open-source game library management tool for Windows that allows you to move your Steam games around your PC and network with ease.

![Game Pipe](http://images.akamai.steamusercontent.com/ugc/311117057225768758/6158E4EDA2CE12E3C07FB652A32DE7D91F0B38E7/?interpolation=lanczos-none&output-format=jpeg&output-quality=95&fit=inside|637:358&composite-to%3D%2A%2C%2A%7C637%3A358&background-color=black)

Game Pipe works by listing your game libraries and providing a drag and drop interface to let you move games between local libraries or even copy games from other local computers running Game Pipe. Once games are moved, Game Pipe ensures that Steam's configuration files are properly managed so that you only need to restart Steam for the game to be ready to play from its new location.

Game Pipe is great for:

    Maximizing the usefulness of a small SSD.
    Quickly pulling games from another local PC without setting up Windows file sharing.
    Backing up your games before reformatting your PC and restoring them afterward.
    Archiving compressed copies of your games on Network Access Storage.
    Relocate a game after a remote install from the Steam mobile app.
    Managing a portable games library on a USB drive.
    Lan Parties! 


Game Pipe will remain an open source project, freely available through GitHub. If you enjoy using Game Pipe, support it on [Greenlight](http://steamcommunity.com/sharedfiles/filedetails/?id=630526624) and consider purchasing Game Pipe when it becomes available.

# How Game Pipe Works
When you start up, Game Pipe automatically finds your Steam installation using the Windows Registry (or asking you to find it if it can't). From there, it searches for a few specific config files in the Steam directory to find the locations of your game libraries.

Within each game library, Game Pipe searches for all of the ".acf" files, which are used by Steam to store information about each of your games. Game Pipe uses these ".acf" files to quickly create a listing of all of your games, including the title and estimated size of the game.

To move a game between libraries in Game Pipe you simply need to drag the game to the desired location to start the move. Behind the scenes Game Pipe will first copy the game directory to it's new location, then it will move the corresponding "appmanifest_<appid>.acf" file to the new library, finally it deletes the game from the original location along with the old ".acf" file. 

Game Pipe also allows you to host your games on your local network, allowing you to connect using another Game Pipe client and copy your games to another computer using your LAN connection (Peer-to-Peer) rather than downloading over the internet. (Note: Game Pipe does not circumvent copy protection in any way. You must still own a game to play it.)

Once you've sufficiently shuffled your library, you'll need to restart Steam. This step is necessary as Steam only reads the ".acf" files from the disk at startup, so it won't find the game in it's new location until you restart. 

#What could possibly go wrong...
Unfortunately, if you attempt to start a moved game without restarting Steam, it adds another minor complication, Steam will actually restore the original ".acf" file at the old location, and depending on the order your libraries appear, it may prefer this old ".acf" which points to the old location of the game (which was deleted) over the new ".acf" file which actually points to an existing directory. 

When this happens you will get a message when trying to launch the game: "Failed to start game (missing executable).", to fix this, you can launch Game Pipe and search for the offending game. If you see two listings for the game, delete the listing that has a size of 0B, then restart Steam. If you only see a listing that says 0B, click the : button on the corner of the Game and choose "Explore to ACF file" from the context menu, then Cut this file; next, click the : button in the corner of the library which you had moved the game to, choose "Open Dir", then Paste the ".acf" file in the new library and restart Steam.


# How is this better than...
One of the most common questions I've seen is "How is this better than Cut-Paste in Explorer?" One of the things that gets overlooked in this question is that you can't simply move your game directory and run the game, because Steam keeps an "appmanifest_<appid>.acf" for each game which tells it which library the game is in. If you move the game without moving the associated ".acf" file, you will get the "Failed to start game (missing executable)" message. 

To get this method to work, you either have to manually find the appropriate ".acf" file and move it too (which means finding the appid of your game) or within Steam you can Delete the game then reinstall it, choosing the new library as the install location. Usually the delete and reinstall option will automatically detect the existing files and skip the download process, however if the game has been updated it will instead force the entire game to redownload (because the files don't match the latest version)

Another method people have found for moving games is to use Steam's "Backup and Restore" process. While this works fine, my testing has shown that this process takes about five times as long as moving a game with Game Pipe ([details of how I tested this are here](https://www.reddit.com/r/pcmasterrace/comments/49cnsc/are_you_tired_of_reinstalling_your_steam_games_i/d0qtq6i))

One last method people have used to move games is symbolic links. This method requires you to simply move the game directory to any location, then create a symbolic directory link in the steam library to the new location. This method works fine, however it can become very disorganized if you move many games this way. This method is used by older tools like "Steam Mover" which predate Steam's support for multiple libraries, as it used to be the only way to move a Steam game outside of Steam's install directory. 

So ultimately, while there are other methods they are either much slower or much more complex. I have used many of these methods in the past myself, however since I developed Game Pipe I personally find I organize my library better (and make more use of my SSD) thanks to the UI. It's also worth noting that I've built in some subtle shortcuts for doing things the manual way, each Game lists it's AppId and provides options to explore to the ".acf" file and to the game's directory, which simplifies the process for those that want/need the manual path.


### Legal Stuff

All trademarks are property of their respective owners in the US and other countries.
