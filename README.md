# Pornhub Photo Downloader

<p>
    <a href="https://github.com/nicengi/PornhubPhotoDownloader/releases"><img alt="Downloads" src="https://img.shields.io/github/downloads/nicengi/PornhubPhotoDownloader/total?style=flat-square" /></a>
    <a href="https://github.com/nicengi/PornhubPhotoDownloader/blob/master/LICENSE"><img alt="License" src="https://img.shields.io/github/license/nicengi/PornhubPhotoDownloader?color=39c5bb&style=flat-square" /></a>
</p>

批量下载 Pornhub 上的相册或照片。

```
示例 .\ppd "https://cn.pornhub.com/album/48071401" -all
```

![00](docs/Images/00.png)

## 参数

| 名称            | 描述                             | 附加                       |
| --------------- | -------------------------------- | -------------------------- |
| -All            | 下载相册的所有照片。             | 仅相册。                   |
| -Dir:{path}     | 指定下载目录。                   |                            |
| -Index:{value}  | 指定起始的下载位置（从零开始）。 | 指定“-All”时选项将被忽略。 |
| -Length:{value} | 指定将下载的照片数量。           | 指定“-All”时选项将被忽略。 |
| -Debug          | 显示调试信息。                   |                            |
| -Info           | 显示更多的调试信息。             | 这是“-Debug”的附加选项。   |
| -Lang:{value}   | 指定区域。                       | -Lang:zh_CN                |

