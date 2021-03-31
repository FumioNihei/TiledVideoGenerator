
# TiledVideoGenerator
マルチモーダルコーパスに収録されている音声・映像データを，実験セッション区間で切り出した上でタイル配置した映像を作成する．

おまけとして，すべての音声・映像データを実験セッション区間で切り出すことも可能．

## 実行環境・依存ソフトウェア
- .NET Core 3.1
- powershell
- ffmpeg
- [PluralEyes](https://www.redgiant.com/products/pluraleyes/)


# 使い方

1. PluralEyesでメディアの同期情報を作成する
1. テキストエディタで`Run.ps1`を開き，いくつかの項目を変更する
1. powershellで`.\Run.ps1`を実行


# FAQ
see -> [FAQ](FAQ.md)

