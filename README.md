# 网易云音乐 Discord Rpc

* 将网易云音乐动态同步到Discord [RPC](https://discordapp.com/rich-presence). 
* Enables Discord [Rich Presence](https://discordapp.com/rich-presence) For Netease Cloud Music.  

## [English ReadMe](#netease-cloud-music-discord-rpc)

### [Download / 下载](https://github.com/Ynng/NetEase-Cloud-Music-DiscordRPC/releases/latest)
  
### Screenshot / 截图
![Screenshot](https://i.imgur.com/7rzkkRb.png)

### 需知
* 这个软件会在你开机的时候自启动, 如果你不需要可以在任务管理器中禁用. 
* 如果你开启了专辑封面，然后快速连续切歌，专辑封面可能会消失**几分钟**。等一下就好了。

  
### 功能
* 同步音乐动态到Discord.
  * 歌名，歌手，专辑，专辑封面，歌曲剩余时间
* 当你运行全屏程序或者其他白名单程序时清除动态. (例如你全屏玩CSGO或者打开了VisualStudio)

### 教程

#### 专辑封面

* 下面两部分需要全部完成才能启用专辑封面

##### Discord 应用
* 你只能给自己的 Discord 应用加 Rich Presence 图片，所以你需要去创建一个自己的Discord应用。

1. 登录 **https://discord.com/developers/applications**
2. 点击右上角 **"New Application"** 按钮
3. 给你的应用起个名字，这个名字会在使用这个软件时显示
![screenshot](https://i.imgur.com/oKiRiqj.png)
1. 把 **Client ID** 复制到secret.txt的第一行
2. 左边打开 **"Rich Presence"** 页
3. 点击 **"Add Image(s)"** 上传你的默认专辑封面

* 我在release.zip的"default images"文件夹里存了些网易云音乐logo

7. 把你刚刚上传的文件名复制到secret.txt的第二行

##### 用户令牌
* 显示专辑封面需要你的Discord用户令牌.
* 自动化账号其实是违反Discord TOS的，我还没听说过谁因为这个被封号，但是**使用后果自负**.

1. 打开**Discord**
2. 按下 **Ctrl+Shift+I** 打开开发者面板
3. 右上角找到 **Application** 标签并打开
4. 面板左边找到 **Local Storage** > **h<span>ttp</span>s://discordapp.com** 并打开
5. 按下 **Ctrl+R** 刷新
6. 列表末端会出现 **token**，复制它
7. 把刚刚复制的 **token** 粘贴到 **secret.txt** 的第三行

* 两边的双引号会被自动去除


#### 应用白名单
白名单应用运行时，网易云音乐的Rich Presence会暂停.
* 要添加软件到白名单, 只需要在windows.txt新增一行输入白名单程序的lpClassName. 查看文档 [FindWindow](https://msdn.microsoft.com/en-us/library/windows/desktop/ms633499(v=vs.85).aspx)
  

# Netease Cloud Music Discord Rpc
  
### Info
* This application will auto launch on system start. If you don't want this, disable it in task manager.
* If you are using album cover and change songs too frequently, the album cover might disappear and stop working for **a few minutes**. You just have to wait.
  
### Feature
* Sync song information to Discord.
  * Title, artists, album name, album cover art & song remaining time
* Clear presence when you are using fullscreen or whitelist Application.
  
### How To

#### Album Cover

* You must complete both of the steps below to get album cover.

##### Discord Application
* You can only add Rich Presence assets to your own Discord Applications, so you'd have to create one.

1. Go to **https://discord.com/developers/applications** and login
2. Click on the **"New Application"** button on the right top corner
3. Give it a name, this name will be displayed as the "game" you are playing when using this software
![screenshot](https://i.imgur.com/oKiRiqj.png)
1. Copy the **Client ID** to line 1 of secret.txt
2. Open the **Rich Presence** tab on the left
3. Click on **"Add Image(s)"** and upload whatever image you want to use as the "blank" album cover

* I've included some netease music logo in the "default images" folder in release.zip if you want to use that

7. Copy the **asset name** and paste it to line 2 of secret.txt

##### Token
* Displaying of the album cover will require your discord user token.
* Automating user accounts is technically against TOS. I have never seen anyone getting banned for this, but **use at your own risk**!

1. Open Discord
2. Press **Ctrl+Shift+I** to show developer tools
3. Navigate to the **Application** tab
4. Select **Local Storage** > **h<span>ttp</span>s://discordapp.com** on the left
5. Press **Ctrl+R** to reload
6. Find **token** at the bottom and copy the value
7. Paste the token into **line 3** of **secret.txt**

* Note that the quotation sign surrounding the "token" can be left on, and will be removed automatically.
"token"


#### Whitelist
When applications in the whitelist is running, Netease Cloud Music's rich presence will pause.
* To add Application to whilelist, edit windows.txt. More info see [FindWindow](https://msdn.microsoft.com/en-us/library/windows/desktop/ms633499(v=vs.85).aspx)
  
