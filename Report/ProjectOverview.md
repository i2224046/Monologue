# プロジェクト概要書: モノ・ローグ (Mono-Logue)

Last Updated: 2026-02-10

本ドキュメントでは、本プロジェクト「モノ・ローグ」（卒業制作）の概要、体験フロー、およびシステム構成の全体像についてまとめます。

## 1. プロジェクト・コンセプト
「持ち物への愛着の再発見」をテーマに、AIを用いて「モノ（物体）」の思考を可視化・言語化するインタラクティブ・インスタレーションです。体験者が提示した物体を認識し、その物体独自の「本音」や「隠れた性格」を動的に生成して語りかけます。

## 2. 体験フロー (User Experience Flow)

1. **待機 (Waiting)**:
   - システムは体験者の入力を待ちます。

2. **撮影 (Capture)**:
   - Unityからのコマンド (`CAPTURE`) により、Webカメラで撮影を行います。
   - フリッカー対策および明るさ調整が行われます。

3. **解析・生成 (Scanning & Analysis)**:
   - **前処理**: 明るさ補正 (Gamma/CLAHE)、背景削除 (`rembg`)。
   - **物体検出**: YOLO-Worldにより物体を検出・切り出し。
   - **詳細分析**: Ollama (Vision Model) により、物体の状態（汚れ、形状、古さなど）を詳細に分析します。

4. **独白生成 (Monologue Generation)**:
   - **性格生成**: 物体の見た目や状態（古びている、機械的、丸いなど）に基づいて、口調や性格付けを動的に調整します。
   - **台詞生成**: DeepSeekにより、物体の「本音」や「ちょっとした不満」「持ち主の秘密」などをウィットに富んだ言葉で生成します。同時に、「全部知ってるスマホ」「字の汚さを知るペン」といった、その物体の性質を表す「捻った異名 (Twisted Name)」も生成されます。

5. **出力 (Output)**:
   - 生成されたテキスト（台詞 + 異名）を標準出力に出力し、Unity側で表示します。
   - *Note: 音声合成 (TTS) 機能は現在コード上で無効化されています。*

## 3. システム構成概要

**Unity (フロントエンド)** と **Python (バックエンド)** が連携して動作します。通信は標準入出力 (stdin/stdout) を使用します。

### Unity (演出・制御)
- 画面表示、ユーザー入力検知、カメラ映像のプレビュー。
- Pythonプロセスを起動・管理し、コマンド送信 (`CAPTURE`) とログ受信を行います。

### Python (知能・生成)
- `Assets/StreamingAssets/main_vision_voice.py` をエントリポイントとして動作。
- **構成ライブラリ/モデル**:
  - **Vision**: YOLO-World (検出), Ollama (詳細分析), rembg (背景削除), OpenCV (画像処理)
  - **LLM**: DeepSeek (台詞生成 - プロンプト制御により動的な性格付け)
  - **TTS**: COEIROINK (※現在はコード上で無効化設定)

## 4. ディレクトリ構成 (関連ファイル)

- **Assets/Scripts/**: Unity側スクリプト
- **Assets/StreamingAssets/**: Pythonスクリプトおよびモデル関連
  - `main_vision_voice.py`: メイン処理ループ
  - `yolo_processor.py`: YOLO検出処理
  - `ollama_client.py`: Ollama連携
  - `deepseek_client.py`: DeepSeek連携 (性格生成ロジック実装)
  - `prompts.py`: プロンプト定義ファイル (性格ロジックの中核)
  - `voice_client.py`: COEIROINK連携 (無効化中)
  - `capture/`: 撮影画像の保存先
  - `config.json`: 設定ファイル
