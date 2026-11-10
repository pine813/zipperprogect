using System; using UnityEngine; namespace BlueRunner { public static class Art {
        public static readonly Color Mint=Hex("67E8FF"),Gold=Hex("B0F3FF"),Coral=Hex("A59AFF");
        public static Sprite White,Disc,Glow,Orb,Drone,Coin,Spike,Sky;
        public static Color Hex(string v){ColorUtility.TryParseHtmlString("#"+v,out Color c);return c;}
        public static void Init(){if(White!=null)return;
            White=Make(2,2,p=>p.Rect(0,0,2,2,Color.white));
            Disc=Make(128,128,p=>p.Ellipse(64,64,63,63,Color.white),FilterMode.Bilinear);
            Glow=Make(128,128,p=>{for(int y=0;y<128;y++)for(int x=0;x<128;x++){float d=Vector2.Distance(new Vector2(x,y),new Vector2(64,64))/64;p.Set(x,y,new Color(1,1,1,Mathf.Pow(Mathf.Max(0,1-d),3)));}},FilterMode.Bilinear);
            Sky=Make(8,256,p=>{for(int y=0;y<256;y++)p.Rect(0,y,8,1,Color.Lerp(Hex("254C89"),Hex("07142F"),y/255f));},FilterMode.Bilinear);
            Orb=Make(128,128,p=>{
                p.Ellipse(64,64,49,49,Hex("071B48"));p.Ellipse(64,66,45,45,Hex("91F2FF"));
                p.Ellipse(65,64,40,40,Hex("245CDB"));p.Ellipse(58,74,32,28,Hex("4EAFFF"));
                p.Poly(Hex("1769CE"),25,65,51,78,69,62,100,71,93,43,63,29,36,42);
                p.Poly(Hex("9BFAFF"),28,83,42,98,65,105,79,94,52,91);
                p.Poly(Hex("D8FFFF"),21,48,49,58,104,83,120,81,87,62,35,42,8,43);
                p.Poly(Hex("3486EF"),12,43,47,51,106,78,115,81,92,65,39,42);
                p.Ellipse(43,88,7,5,Color.white);
            },FilterMode.Bilinear);
            Drone=Make(128,96,p=>{
                p.Poly(Hex("08162E"),5,48,28,73,90,73,123,45,103,16,24,16);
                p.Poly(Hex("5569B0"),12,49,33,67,88,67,116,45,96,23,27,23);
                p.Poly(Hex("98B6F4"),14,49,34,67,88,67,100,55,39,54);
                p.Rect(42,32,47,16,Hex("122854"));p.Rect(49,36,32,7,Hex("ED9CFF"));
                p.Poly(Hex("54DAFF"),0,23,30,28,26,39,8,35);p.Poly(Hex("54DAFF"),99,28,126,23,120,35,102,39);
            },FilterMode.Bilinear);
            Coin=Make(64,64,p=>{p.Poly(Hex("173B83"),32,1,57,32,32,63,7,32);p.Poly(Mint,32,7,50,32,32,57,14,32);p.Poly(Color.white,32,14,32,49,22,32);},FilterMode.Bilinear);
            Spike=Make(64,64,p=>{p.Poly(Hex("405FBC"),3,0,32,62,61,0);p.Poly(Hex("BAEFFF"),3,0,32,62,26,0);},FilterMode.Bilinear);
        }
        public static SpriteRenderer Sprite(string name, Sprite sprite, Transform parent, Vector2 position, Vector2 size, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }

        public static SpriteRenderer Box(string name, Transform parent, Vector2 position, Vector2 size, Color color, int order)
            => Sprite(name, White, parent, position, size, color, order);

        static Sprite Make(int width, int height, Action<Painter> draw, FilterMode filter = FilterMode.Point)
        {
            var painter = new Painter(width, height);
            draw(painter);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { filterMode = filter, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels(painter.Pixels);
            texture.Apply();
            return UnityEngine.Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(.5f, .5f), width);
        }

        sealed class Painter
        {
            public readonly Color[] Pixels;
            readonly int width, height;
            public Painter(int w, int h) { width = w; height = h; Pixels = new Color[w * h]; }
            public void Set(int x, int y, Color c) { if (x >= 0 && y >= 0 && x < width && y < height) Pixels[y * width + x] = c; }
            public void Rect(int x, int y, int w, int h, Color c) { for (int j = y; j < y + h; j++) for (int i = x; i < x + w; i++) Set(i, j, c); }
            public void Ellipse(int cx, int cy, int rx, int ry, Color c)
            {
                for (int y = cy - ry; y <= cy + ry; y++) for (int x = cx - rx; x <= cx + rx; x++)
                    if ((x-cx)*(x-cx)/(float)(rx*rx) + (y-cy)*(y-cy)/(float)(ry*ry) <= 1) Set(x,y,c);
            }
            public void Poly(Color c, params int[] points)
            {
                for (int y = 0; y < height; y++) for (int x = 0; x < width; x++) {
                    bool inside = false;
                    for (int i = 0, j = points.Length - 2; i < points.Length; j = i, i += 2)
                        if ((points[i+1] > y) != (points[j+1] > y) && x < (points[j]-points[i]) * (y-points[i+1]) / (float)(points[j+1]-points[i+1]) + points[i]) inside = !inside;
                    if (inside) Set(x,y,c);
                }
            }
        }
    }
}
