# モノ・ローグ

この作品は、「持ち物への愛着の再発見」 をテーマにしています。小学生時代から使っている水筒、祖母から受け継いだ財布——家族や親友よりも長い時間を共有してきた「モノ」たちが、もし喋り出したら何を語るのか...？そんな想いから生まれました。

## 特徴

- **物体検出**: YOLO-World を使用して、幅広い個人の持ち物を検出・識別します。
- **ペルソナ生成**: 検出された物体に基づいて、DeepSeek (OpenAI API互換) を使用して独自の性格と対話を生成します。
- **音声合成**: OpenAIのText-to-Speech APIを使用して、生成されたテキストを音声に変換します。
- **背景削除**: `rembg` を使用して、ビデオフィードから物体を切り抜きます。
- **Unity フロントエンド**: Unityで構築されたリッチなビジュアルインターフェースで、カメラ映像、認識された物体、生成されたテキストを表示します。

## 必要要件

- Python 3.10以上
- Unity 2022.3以上 (フロントエンド用)
- Webカメラ

### Python 依存ライブラリ

必要なPythonパッケージをインストールします：

```bash
pip install -r requirements.txt
```

### 外部モデル

- **Ollama**: [Ollama](https://ollama.com/) をインストールし、ローカルで `qwen3-vl` モデルを実行できる状態にする必要があります（補助的な分析に使用）。
  ```bash
  ollama pull qwen3-vl
  ```

## セットアップ

1.  **リポジトリのクローン**:
    ```bash
    git clone https://github.com/yourusername/Sotsusei1107.git
    cd Sotsusei1107
    ```

2.  **環境変数**:
    プロジェクトのルートディレクトリに `.env` ファイルを作成し、APIキーを設定してください：
    ```env
    DEEPSEEK_API_KEY=your_deepseek_api_key
    OPENAI_API_KEY=your_openai_api_key
    ```

3.  **バックエンドの実行**:
    メインのPythonスクリプトを実行して、ビジョンとAI処理のループを開始します。
    ```bash
    python main_vision_voice.py
    ```
    *(注: 最新の構成に合わせてスクリプト名を適宜変更してください)*

4.  **フロントエンドの実行**:
    Unityでプロジェクトを開き、Main Sceneを再生します。

## ライセンス

このプロジェクトは **GNU Affero General Public License v3.0 (AGPL-3.0)** の下でライセンスされています。
詳細は [LICENSE](LICENSE) ファイルをご確認ください。

**なぜ AGPL-3.0 なのか？**
このプロジェクトは [YOLO-World](https://github.com/ultralytics/ultralytics) (AGPL-3.0) を使用しています。そのため、その派生著作物である本プロジェクトも、コピーレフトの要件に従い AGPL-3.0 で公開する必要があります。

## 謝辞

このプロジェクトで使用されているオープンソースライブラリの一覧については、[THIRD_PARTY_LICENSES.md](THIRD_PARTY_LICENSES.md) を参照してください。