import os
import json
import requests
import ffmpeg
import time
import sys
import shutil  # 追加
from watchdog.observers import Observer  # 追加
from watchdog.events import FileSystemEventHandler  # 追加

API_SERVER = "http://127.0.0.1:50032"
SPEAKER_UUID = "b28bb401-bc43-c9c7-77e4-77a2bbb4b283"
STYLE_ID = 3
SILENT_FILE = "assets/silent.wav"  # 無音ファイルのパスを定数化


def synthesis(text: str):
    """
    文字列を音声化する (変更なし)
    """
    query = {
        "speakerUuid": SPEAKER_UUID,
        "styleId": STYLE_ID,
        "text": text,
        "speedScale": 1.25,
        "volumeScale": 1.0,
        "prosodyDetail": [],
        "pitchScale": 0.0,
        "intonationScale": 1.0,
        "prePhonemeLength": 0.1,
        "postPhonemeLength": 0.5,
        "outputSamplingRate": 24000,
    }

    response = requests.post(
        f"{API_SERVER}/v1/synthesis",
        headers={"Content-Type": "application/json"},
        data=json.dumps(query),
    )
    response.raise_for_status()
    return response.content


def append_audio(audio1: str, audio2: str):
    """
    audio1の後ろにaudio2を結合する (一時ファイル名の競合対策を追加)
    """
    # 競合を避けるため、PIDと時刻を使った一時ファイル名に変更
    old_file = f"{audio1}.{os.getpid()}.{int(time.time())}.old"
    
    if not os.path.exists(audio1):
        raise FileNotFoundError(f"Base audio file not found: {audio1}")

    os.rename(audio1, old_file)
    
    try:
        (
            ffmpeg.concat(ffmpeg.input(old_file), ffmpeg.input(audio2), v=0, a=1)
            .output(audio1)
            .run(overwrite_output=True, quiet=True) # quiet=True を追加してもよい
        )
        os.remove(old_file)
    except Exception as e:
        print(f"ffmpeg error during concat: {e}")
        # エラー時は古いファイルを復元
        os.rename(old_file, audio1)
        raise # エラーを呼び出し元に通知


def process_file(input_path: str):
    """
    単一のテキストファイルを読み込み、音声ファイルに変換する
    """
    
    # 出力ファイル名と一時ファイル名を決定
    base_name = os.path.splitext(os.path.basename(input_path))[0]
    output_dir = os.path.dirname(input_path)
    output_path = os.path.join(output_dir, f"{base_name}.wav")
    temp_file = os.path.join(output_dir, f"{base_name}.temp.wav")

    print(f"Processing {input_path} -> {output_path} ...")

    # 既に出力ファイルが存在する場合はスキップ
    if os.path.exists(output_path):
        print(f"Output file {output_path} already exists. Skipping.")
        return

    try:
        with open(input_path, "r", encoding="utf-8") as f:
            count = 0
            for line in f:
                line = line.strip()

                if line.startswith("#") or line.startswith("//") or line == "":
                    continue

                if line == "<<silent>>":
                    if not os.path.exists(SILENT_FILE):
                        print(f"Error: Silent file not found at {SILENT_FILE}. Skipping line.")
                        continue
                    
                    if count == 0:
                        # 最初の音声が無音の場合
                        shutil.copy(SILENT_FILE, output_path)
                    else:
                        # 無音ファイルを追加 (assets/silent.wav は削除しない)
                        append_audio(output_path, SILENT_FILE)
                    
                    count += 1
                    continue

                # テキストを音声化
                audio = synthesis(line)

                if count == 0:
                    # 最初の音声
                    with open(output_path, "wb") as f_temp:
                        f_temp.write(audio)
                else:
                    # 2回目以降は一時ファイルに書き込んでから結合
                    with open(temp_file, "wb") as f_temp:
                        f_temp.write(audio)
                    append_audio(output_path, temp_file)
                    os.remove(temp_file) # 一時ファイルを削除

                count += 1
        
        print(f"Finished processing {output_path}")

    except Exception as e:
        print(f"Error processing file {input_path}: {e}")
        # エラー発生時に一時ファイルが残っていれば削除
        if os.path.exists(temp_file):
            os.remove(temp_file)
    finally:
        # 正常終了後、入力ファイルを移動するなどの処理も可能
        pass


class TextFileHandler(FileSystemEventHandler):
    """
    ファイル作成イベントを監視するハンドラ
    """
    def on_created(self, event):
        if event.is_directory or not event.src_path.endswith(".txt"):
            return

        print(f"New file detected: {event.src_path}")
        
        # ファイルが完全に書き込まれるまで簡易的に待機
        time.sleep(1.5) 

        try:
            process_file(event.src_path)
        except Exception as e:
            print(f"Failed to process {event.src_path}: {e}")


if __name__ == "__main__":
    # 監視対象フォルダを引数で指定。指定がなければカレントディレクトリ ('.')
    watch_dir = sys.argv[1] if len(sys.argv) > 1 else "."
    
    if not os.path.isdir(watch_dir):
        print(f"Error: Directory not found - {watch_dir}")
        sys.exit(1)

    if not os.path.exists(SILENT_FILE):
        print(f"Warning: Silent file '{SILENT_FILE}' not found. '<<silent>>' directive will not work.")

    print(f"Watching directory for new .txt files: {os.path.abspath(watch_dir)}")

    event_handler = TextFileHandler()
    observer = Observer()
    # recursive=False に設定し、サブディレクトリは監視しない
    observer.schedule(event_handler, watch_dir, recursive=False) 

    observer.start()
    try:
        while True:
            time.sleep(1)
    except KeyboardInterrupt:
        print("Stopping observer...")
        observer.stop()
    
    observer.join()
    print("Observer stopped.")