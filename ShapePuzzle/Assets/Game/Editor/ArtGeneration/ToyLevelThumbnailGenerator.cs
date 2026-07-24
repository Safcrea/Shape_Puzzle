using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ToyPuzzle.Editor
{
    public static class ToyLevelThumbnailGenerator
    {
        public const string ThumbnailRoot = "Assets/Game/Art/Generated/Thumbnails";
        private const int Size = 256;

        public static int GenerateThumbnails()
        {
            EnsureFolder(ThumbnailRoot);
            LevelCatalog catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>("Assets/Game/Data/Levels/Generated/LevelCatalog.asset");
            ToyPalette palette = AssetDatabase.LoadAssetAtPath<ToyPalette>(ToyArtGenerator.PalettePath);
            if (catalog == null || palette == null)
            {
                Debug.LogWarning("Thumbnail generation requires LevelCatalog and ToyPalette. Generate levels and art first.");
                return 0;
            }
            int count = 0;
            for (int i = 0; i < catalog.Count; i++)
            {
                RuntimeLevelData runtime = catalog.GetByIndex(i);
                if (runtime == null || runtime.Level == null) continue;
                Generate(runtime.Level, palette);
                count++;
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureImporters();
            Debug.Log("Generated " + count + " deterministic target thumbnails.");
            return count;
        }

        private static void Generate(LevelDefinition level, ToyPalette palette)
        {
            string assetPath=ThumbnailRoot+"/level_"+level.levelNumber.ToString("000")+".png";
            string fullPath=Path.GetFullPath(assetPath);
            if (PreserveAuthoredThumbnail(fullPath)) return;

            List<PiecePaint> pieces = new List<PiecePaint>();
            int minX=int.MaxValue,minY=int.MaxValue,maxX=int.MinValue,maxY=int.MinValue;
            for(int i=0;i<level.pieces.Length;i++)
            {
                PieceDefinition piece=level.pieces[i]; GridCoordinate[] occupied=GridMath.GetOccupiedCells(piece,piece.TargetPose); Color color=palette.ResolvePieceColor(piece.colorId);
                if(occupied.Length==0)continue;
                int pieceMinX=int.MaxValue,pieceMinY=int.MaxValue,pieceMaxX=int.MinValue,pieceMaxY=int.MinValue;
                for(int j=0;j<occupied.Length;j++){ GridCoordinate cell=occupied[j]; pieceMinX=Mathf.Min(pieceMinX,cell.x);pieceMinY=Mathf.Min(pieceMinY,cell.y);pieceMaxX=Mathf.Max(pieceMaxX,cell.x);pieceMaxY=Mathf.Max(pieceMaxY,cell.y);minX=Mathf.Min(minX,cell.x);minY=Mathf.Min(minY,cell.y);maxX=Mathf.Max(maxX,cell.x);maxY=Mathf.Max(maxY,cell.y); }
                pieces.Add(new PiecePaint(piece,occupied,color,pieceMinX,pieceMinY,pieceMaxX,pieceMaxY));
            }
            if(pieces.Count==0)return;
            int width=maxX-minX+1,height=maxY-minY+1; float cellSize=Mathf.Min(208f/width,178f/height); float originX=(Size-width*cellSize)*.5f; float originY=(Size-height*cellSize)*.5f;
            Color[] pixels=new Color[Size*Size];
            for(int y=0;y<Size;y++) for(int x=0;x<Size;x++)
            {
                float boardLight=.90f+Mathf.Sin((x*17f+y*11f)*.11f)*.008f;
                Color final=palette.boardFrame*boardLight; final.a=1f;
                float gridX=minX+(x-originX)/cellSize; float gridY=minY+(y-originY)/cellSize;
                for(int p=0;p<pieces.Count;p++)
                {
                    PiecePaint paint=pieces[p];
                    if(!Contains(paint,gridX,gridY))continue;
                    float u=(gridX-paint.minX)/Mathf.Max(1,paint.maxX-paint.minX+1);
                    float v=(gridY-paint.minY)/Mathf.Max(1,paint.maxY-paint.minY+1);
                    float lighting=.88f+v*.10f-u*.035f;
                    final=paint.color*lighting; final.a=1f; break;
                }
                pixels[y*Size+x]=final;
            }
            Texture2D texture=new Texture2D(Size,Size,TextureFormat.RGBA32,false); texture.SetPixels(pixels); texture.Apply(false,false); byte[] bytes=texture.EncodeToPNG(); Object.DestroyImmediate(texture);
            if(!File.Exists(fullPath)||!BytesEqual(File.ReadAllBytes(fullPath),bytes))File.WriteAllBytes(fullPath,bytes);
        }

        private static bool PreserveAuthoredThumbnail(string fullPath)
        {
            if (!File.Exists(fullPath)) return false;
            byte[] bytes = File.ReadAllBytes(fullPath);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                return texture.LoadImage(bytes, true) && (texture.width > Size || texture.height > Size);
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        private static void ConfigureImporters()
        {
            string[] guids=AssetDatabase.FindAssets("t:Texture2D",new[]{ThumbnailRoot});
            for(int i=0;i<guids.Length;i++)
            {
                string path=AssetDatabase.GUIDToAssetPath(guids[i]); TextureImporter importer=AssetImporter.GetAtPath(path) as TextureImporter; if(importer==null)continue;
                if(importer.textureType==TextureImporterType.Sprite&&!importer.mipmapEnabled&&!importer.isReadable&&importer.maxTextureSize==256)continue;
                importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single; importer.spritePixelsPerUnit=100f; importer.mipmapEnabled=false; importer.isReadable=false; importer.alphaIsTransparency=true; importer.maxTextureSize=256; importer.textureCompression=TextureImporterCompression.Compressed; importer.SaveAndReimport();
            }
        }

        private static bool BytesEqual(byte[] first,byte[] second){if(first.Length!=second.Length)return false;for(int i=0;i<first.Length;i++)if(first[i]!=second[i])return false;return true;}
        private static void EnsureFolder(string path){if(AssetDatabase.IsValidFolder(path))return;int split=path.LastIndexOf('/');string parent=path.Substring(0,split);EnsureFolder(parent);AssetDatabase.CreateFolder(parent,path.Substring(split+1));}

        private static bool Contains(PiecePaint piece,float x,float y)
        {
            float width=Mathf.Max(1,piece.maxX-piece.minX+1); float height=Mathf.Max(1,piece.maxY-piece.minY+1);
            float u=(x-piece.minX)/width; float v=(y-piece.minY)/height;
            if(u<0f||u>1f||v<0f||v>1f)return false;
            switch(piece.definition.shapeType)
            {
                case PieceShapeType.Circle: return new Vector2((u-.5f)/.47f,(v-.5f)/.47f).sqrMagnitude<=1f;
                case PieceShapeType.Ring:
                    float radius=new Vector2(u-.5f,v-.5f).magnitude; return radius>=.24f&&radius<=.48f;
                case PieceShapeType.Capsule:
                    if(width>=height)return CapsuleContains(u,v,true); return CapsuleContains(u,v,false);
                case PieceShapeType.Triangle:
                case PieceShapeType.Wedge: return TriangleContains(u,v,piece.definition.targetRotation);
                case PieceShapeType.Trapezoid: return v>=.04f&&v<=.96f&&u>=Mathf.Lerp(.05f,.24f,v)&&u<=Mathf.Lerp(.95f,.76f,v);
                case PieceShapeType.Semicircle:
                    return v>=.06f&&new Vector2((u-.5f)/.47f,(v-.06f)/.90f).sqrMagnitude<=1f;
                case PieceShapeType.QuarterCircle:
                    return u>=.04f&&v>=.04f&&new Vector2((u-.04f)/.94f,(v-.04f)/.94f).sqrMagnitude<=1f;
            }

            if(!UsesCellComposition(piece.definition.shapeType))return RoundedRectContains(u,v,.10f);
            for(int i=0;i<piece.cells.Length;i++)
            {
                GridCoordinate cell=piece.cells[i];
                if(x>=cell.x&&x<=cell.x+1f&&y>=cell.y&&y<=cell.y+1f)return true;
            }
            return false;
        }

        private static bool CapsuleContains(float u,float v,bool horizontal)
        {
            if(horizontal)
            {
                if(u>=.25f&&u<=.75f&&v>=.04f&&v<=.96f)return true;
                float cx=u<.5f?.25f:.75f; return new Vector2((u-cx)/.25f,(v-.5f)/.46f).sqrMagnitude<=1f;
            }
            if(v>=.25f&&v<=.75f&&u>=.04f&&u<=.96f)return true;
            float cy=v<.5f?.25f:.75f; return new Vector2((u-.5f)/.46f,(v-cy)/.25f).sqrMagnitude<=1f;
        }

        private static bool TriangleContains(float u,float v,int rotation)
        {
            int normalized=((rotation%360)+360)%360;
            Vector2 a,b,c;
            if(normalized==90){a=new Vector2(.96f,.5f);b=new Vector2(.05f,.05f);c=new Vector2(.05f,.95f);}
            else if(normalized==180){a=new Vector2(.5f,.04f);b=new Vector2(.95f,.95f);c=new Vector2(.05f,.95f);}
            else if(normalized==270){a=new Vector2(.04f,.5f);b=new Vector2(.95f,.95f);c=new Vector2(.95f,.05f);}
            else{a=new Vector2(.5f,.96f);b=new Vector2(.05f,.05f);c=new Vector2(.95f,.05f);}
            float d1=Sign(new Vector2(u,v),a,b),d2=Sign(new Vector2(u,v),b,c),d3=Sign(new Vector2(u,v),c,a);
            bool negative=d1<0f||d2<0f||d3<0f,positive=d1>0f||d2>0f||d3>0f; return !(negative&&positive);
        }

        private static float Sign(Vector2 p,Vector2 a,Vector2 b){return (p.x-b.x)*(a.y-b.y)-(a.x-b.x)*(p.y-b.y);}
        private static bool RoundedRectContains(float u,float v,float radius)
        {
            float dx=Mathf.Max(Mathf.Abs(u-.5f)-(.5f-radius),0f); float dy=Mathf.Max(Mathf.Abs(v-.5f)-(.5f-radius),0f); return dx*dx+dy*dy<=radius*radius;
        }
        private static bool UsesCellComposition(PieceShapeType shape)
        {
            return shape==PieceShapeType.LShape||shape==PieceShapeType.TShape||shape==PieceShapeType.UShape||shape==PieceShapeType.ZShape||shape==PieceShapeType.CrossShape||shape==PieceShapeType.Polyomino||shape==PieceShapeType.CustomGridFootprint||shape==PieceShapeType.CustomPolygon;
        }

        private readonly struct PiecePaint
        {
            public readonly PieceDefinition definition; public readonly GridCoordinate[] cells; public readonly Color color; public readonly int minX,minY,maxX,maxY;
            public PiecePaint(PieceDefinition definition,GridCoordinate[] cells,Color color,int minX,int minY,int maxX,int maxY){this.definition=definition;this.cells=cells;this.color=color;this.minX=minX;this.minY=minY;this.maxX=maxX;this.maxY=maxY;}
        }
    }
}
