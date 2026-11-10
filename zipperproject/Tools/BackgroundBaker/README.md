# Background Baker

这是《星渊 · 归途》四张主题天空的绘制源工具，与 Unity 工程运行时使用同一套 Van Gogh 笔触逻辑（rt.js）。

直接用浏览器打开 g-bake.html?theme=0&seed=92731，页面会生成 1920×1080 主题天空。

- 主题：	heme=0..3，对应星夜 / 新宇宙 / 月潮 / 琥珀。
- 种子：seed= 可更换星点、光晕与运笔的随机种子。
- 预览：追加 &preview=1 可在背景上预览黑洞、归航阵与自转星体。

烘焙成 Unity 资源时，把截图保存为：

    StarryAbyss/Assets/Resources/Art/Background0.png
    StarryAbyss/Assets/Resources/Art/Background1.png
    StarryAbyss/Assets/Resources/Art/Background2.png
    StarryAbyss/Assets/Resources/Art/Background3.png

Unity 的 StarryTextureImport 会自动把 Resources/Art/ 中贴图设为不压缩、双线性过滤。
