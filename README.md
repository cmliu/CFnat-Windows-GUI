# CFnat-Windows-GUI
首先声明，这款软件并非我原创开发，而是目前了解到由一位神秘大佬在 [CF中转IP 频道](https://t.me/CF_NAT/38840) 发布的一款实时筛选 Cloudflare 数据中心的软件。我所编写的GUI是在这位开发者的原始版本基础上进行的**二次开发**。

CFNAT 是一款自动查找并优化 Cloudflare IP 转发的工具，旨在**解决泛播 IP 路由不稳定的问题**。如果你曾找到过速度不错的 Cloudflare IP，CFNAT 能帮助你快速筛选出最佳 IP 并实现端口转发，从而提升网络使用体验。因此，这款工具对于**移动、广电网络用户来说尤为明显**！

Telegram交流群：[@CMLiussss](https://t.me/CMLiussss)，YouTuBe：[CM喂饭 干货满满](https://www.youtube.com/@CMLiussss)

# 免责声明
CFnat-Windows-GUI项目仅供教育、研究和安全测试目的而设计和开发。本项目旨在为安全研究人员、学术界人士及技术爱好者提供一个探索和实践网络通信技术的工具。
在下载和使用本项目代码时，使用者必须严格遵守其所适用的法律和规定。使用者有责任确保其行为符合所在地区的法律框架、规章制度及其他相关规定。

### 使用条款

- **教育与研究用途**：本软件仅可用于网络技术和编程领域的学习、研究和安全测试。
- **禁止非法使用**：严禁将CFnat-Windows-GUI用于任何非法活动或违反使用者所在地区法律法规的行为。
- **使用时限**：基于学习和研究目的，建议用户在完成研究或学习后，或在安装后的**24小时内，删除本软件及所有相关文件。**
- **免责声明**：CFnat-Windows-GUI的创建者和贡献者不对因使用或滥用本软件而导致的任何损害或法律问题负责。
- **用户责任**：**用户对使用本软件的方式以及由此产生的任何后果完全负责。**
- **无技术支持**：本软件的创建者不提供任何技术支持或使用协助。
- **知情同意**：使用CFnat-Windows-GUI即表示您已阅读并理解本免责声明，并同意受其条款的约束。

**请记住**：本软件的主要目的是促进学习、研究和安全测试。作者不支持或认可任何其他用途。使用者应当在合法和负责任的前提下使用本工具。

---

![GUI](./gui.png)

## GUI文件结构
```shell
CFnat Windows GUI.exe    #GUI本体
cfnat-windows-386.exe    #cfnat x86_32位 原程序本体
cfnat-windows-amd64.exe  #cfnat x86_64位 原程序本体
cfnat-windows-arm.exe    #cfnat arm_32位 原程序本体
cfnat-windows-arm64.exe  #cfnat arm_64位 原程序本体
cfnat-windows7-386.exe   #cfnat Win7_x86_32位 原程序本体 Win7专用
cfnat-windows7-amd64.exe #cfnat Win7_x86_64位 原程序本体 Win7专用
ips-v4.txt               #IPv4库
ips-v6.txt               #IPv6库
locations.json           #CF数据中心json文件
```

# 致谢
我自己的脑洞，ChatGPT
