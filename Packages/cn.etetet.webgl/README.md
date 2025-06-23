# 安装步骤
1. 安装cn.etetet.statesync包，安装方法请看et 运行指南，并且能正常运行
2. 切换Unity工程到WebGL模式
3. 安装cn.etetet.webgl包
4. 给DotNet的ET.Hotfix工程加上UNITY_WEBGL宏（右键给DotNet的ET.Hotfix工程，properties->Debug->Define constants加上UNITY_WEBGL，注意分号分隔）
5. 整个WebGL包安装完成
   
# 打包过程
1. 点击HybridCLR -> Installer，点击安装，等待安装完成
2. 用IDE进行编译，编译整个Unity工程
3. 点击HybridCLR -> Generate -> All
4. 点击HybridCLR -> CopyAotDlls，这一步会把需要补充元数据的dll复制到'Assets/Bundles/AotDlls'目录
5. 打开YooAsset -> AssetBundle Builder窗口，按照以下步骤操作:
   ①BuildPipeline : '**ScriptableBuildPipeline**'
   ②BuildMode : '**IncrementalBuild**'
   ③CopyBuildinFileOption : '**ClearAndCopyAll**'
   ④点击'**Click Build**'

6. ET.YooAssets Package中的Resources目录下的YooConfig EPlayMode选择'WebPlayMode', Url填写连接的http地址(根据下面配置的web地址填写，例如: http://192.168.2.106)
7. 打开Unity菜单 -> ET -> BuildTool，点击'BuildPackage'，Windows下生成在'ET/Release/ET'里面

另：**请自行研究**YooAsset包管理库的使用方式([YooAsset官网](https://www.yooasset.com/))


# WebGL服务器运行 
1. 启动web服务器，我们可以使用IIS，IIS安装请全部勾选安装。新建一个网站，目录是ET/Release/ET  假设url设置为http://192.168.2.106
2. copy Packages/cn.etetet.webgl/IIS/web.config到ET/Release/ET目录，这是配置IIS的配置  
3. 浏览器输入http://192.168.2.106 运行
4. 注意url地址是否配置正确，很多人运行的时候发现下载bundle失败，就傻了，可以复制bundle的url到浏览器运行，看看能否下载成功，看看地址跟iis中是否一样，不一样自己调整即可
5. 资源的url地址是在ET.YooAssets Package中的Resources目录下的YooConfig配置的，相对地址是在ResourcesComponent中写好的，有需要自己修改

# 微信小游戏真机运行 （这个需要自己研究，et9并不带小游戏插件，因为大家可能使用团结，步骤并不一样，请自己摸索小游戏打包）
1. 注意真机不能用http，所以需要大家装nginx，装上证书做一次htts转发，转成http到服务器
2. 这里主要得难点在于需要搞一个nginx转发，这个得大家自己去配置了，routerManager 跟router得对外地址都要用nginx转发
3. RouterAddressComponentSystem中GetAllRouter方法中http改成https
4. WChannel_WebGL文件WChannel构造函数中，new Uri($"ws://{ipEndPoint}"改成new Uri($"wss://{nginx_http_url}/{ipEndPoint}", 意思是用wss连接nginx，然后nginx中配置根据路径把http转发给对应得ipEndPoint