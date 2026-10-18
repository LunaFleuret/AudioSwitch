# AudioSwitch

Windowsのオーディオ出力先をワンクリックで切り替える軽量ツールです。

## 動作要件

- **対応OS**: Windows 10 / 11 (64-bit)
- **実行環境**: [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
- **ビルド環境**（ソースコードからビルドする場合）: [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## ビルド手順

リポジトリルートで `build.bat` を実行するか、以下のコマンドを実行します。

```powershell
dotnet publish AudioSwitch.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish
```

実行ファイルは `publish/AudioSwitch.exe` に生成されます。

## 設定

アプリと同じフォルダにある `config.json` で切り替え対象の機器を指定します。

- 初回起動時に `config.json` が存在しない場合は、接続中の再生デバイスを自動検出して `config.json` を自動生成します。
- 手動で設定する場合は `config.example.json` を `config.json` にコピーして編集してください。

```json
{
  "devices": [
    {
      "key": "speakers",
      "displayName": "スピーカー",
      "searchKeyword": "Speakers",
      "iconIndex": -3016
    },
    {
      "key": "headphones",
      "displayName": "ヘッドホン",
      "searchKeyword": "Headphones",
      "iconIndex": -3012
    }
  ]
}
```

## 使い方

- **左クリック（通常実行）**: 登録されている機器へ順番に切り替えます。
- **タスクバーのジャンプリスト**: `setup_shortcut.bat` でショートカットを作成してタスクバーにピン留めすると、右クリックメニューから任意の機器を直接選択できます。
