
# TiledVideoGenerator
マルチモーダルコーパスに収録されている音声・映像データを，実験セッション区間で切り出した上でタイル配置した映像を作成する．

おまけとして，すべての音声・映像データを実験セッション区間で切り出すことも可能．

## 実行環境・依存ソフトウェア
- .NET Core 3.1
- powershell
- ffmpeg
- [PluralEyes](https://www.maxon.net/ja/red-giant-complete/pluraleyes/)


# サンプルでの実行

**サンプルはまだ用意してないよ**

サンプルのデータは以下．

- `./sample/sync.xml`: `PluralEyes`のメディア同期結果の出力ファイル
- `./sample/targets.txt`: セッション区間のリスト
- `./sample/medias`: 同期をとるための映像や音声データ（**準備まだ**）

実行手順は以下．

1. `TiledVideoGenerator`ディレクトリを`C:\`直下に配置する
    - `PluralEyes`は取り込んだメディアファイルのパスを絶対パスとして扱う都合による
1. テキストエディタで`.\Run.ps1`を開き，`ffmpeg`のパス等を変更する
1. powershellで`.\Run.ps1`を実行

# Tips
see -> [Tips](/misc/Tips.md)

