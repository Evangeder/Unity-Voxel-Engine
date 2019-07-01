Modding - Blocks

First of all, you can change textures and add new ones.
If you create a server, all players WILL download your pack.

This is filename of texture file. It HAS to be in PNG format.
Texture_File=Blocks.png

You also need to type the WIDTH and HEIGHT of the textures.
Texture_Width=128
Texture_Height=128

Last but not least, Tile Size.
Your texture file can be only a square, not rectangle!
If you add new blocks, you calculate the TileSize as (1 / Amount of tiles in a row)
TileSize=0.25

Blocktypes line is for defining how many blocks you wish to have ingame.
Remember, that counting goes from 0!
Blocktypes=33

Next, blocks themselves.
Number in bracket is your BLOCK ID. You may have only one block per ID.
[0]

Solid determines if surrounding blocks should render the faces towards this block.
Usually used for transparent blocks or liquids.
Solid=False

Now, there are two types of texture lines.
First is Texture
Texture=0,0
This defines just only ONE texture for all faces of the block.
The second declaration is Texture_Up, Texture_Down... etc
Texture_Up=2,0
Texture_Down=1,0
Texture_North=3,0
Texture_South=3,0
Texture_East=3,0
Texture_West=3,0
With this, you can set up different textures on every face.
But remember, if you use it, you have to FILL EVERY DECLARATION of texture per face, even if textures ARE repeating.

Now, decide if your block uses physics.
Uses_Physics=False
If it doesn't, set it to False, since the more True blocks are there, the more the game lags (or may lag).

Lastly, Physics_Time. This is the time in MILLISECONDS between calling physics update on said block.
Never, ever use 0 with block that has ENABLED physics. (you can, but ofc this will kill performace)
Start with at least 500 and then tweak it to your needs.
Physics_Time=0