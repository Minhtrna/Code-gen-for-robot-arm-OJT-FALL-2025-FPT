"""OOP wrapper for Google GenAI image annotation (Gemini Robotics).

This module provides a `GeminiImageAnnotator` class that wraps the
`google.genai` client to send an image plus a prompt to the
`gemini-robotics-er-1.5-preview` model and return the model's output.

Usage example (see bottom of file):
  export GENAI_API_KEY=...
  python API_Gemini.py path/to/image.png

The class focuses on small, clear responsibilities:
 - initialize client
 - load image bytes
 - call generate_content and try to parse JSON from the response
"""
from __future__ import annotations

import json
import os
import re
try:
    import imghdr  # type: ignore
except Exception:
    imghdr = None

import mimetypes
import getpass
from typing import Optional, Any, Dict, List

from google import genai
from google.genai import types

# Default model used in the original snippet
DEFAULT_MODEL_ID = "gemini-robotics-er-1.5-preview"


class GeminiImageAnnotator:
    """A small wrapper around google.genai Client for image annotation.

    Responsibilities:
      - Initialize the GenAI client with an API key
      - Load image bytes from disk
      - Send image + prompt to the model and return parsed JSON (if any)

    Inputs / outputs (contract):
      - init(api_key, model_id, temperature, thinking_budget)
      - annotate_image(prompt, image_path=None, image_bytes=None) -> dict

    Error modes:
      - Raises ValueError when API key is missing
      - Raises FileNotFoundError when image path does not exist
      - Returns {'raw_text': ...} when JSON parsing fails
    """

    def __init__(
        self,
        api_key: str,
        model_id: str = DEFAULT_MODEL_ID,
        temperature: float = 0.5,
        thinking_budget: int = 0,
    ) -> None:
        if not api_key:
            raise ValueError("api_key is required")

        self.api_key = api_key
        self.client = genai.Client(api_key=api_key)
        self.model_id = model_id
        self.temperature = temperature
        self.thinking_budget = thinking_budget

    def load_image(self, path: str) -> bytes:
        """Read an image file from disk and return bytes.

        Raises FileNotFoundError when path is missing.
        """
        if not path or not os.path.isfile(path):
            raise FileNotFoundError(f"Image file not found: {path}")
        with open(path, "rb") as f:
            return f.read()

    @staticmethod
    def _extract_json_from_text(text: str) -> Optional[Any]:
        """Try to extract the first JSON array or object from `text`.

        This handles cases where the model prints some explanatory text
        surrounding the JSON. It searches for the first top-level JSON
        array/object and attempts to parse it.
        """
        if not text:
            return None

        # Fast path: if the whole text is valid JSON
        try:
            return json.loads(text)
        except Exception:
            pass

        # Look for a JSON structure by finding the first `[` or `{` and the
        # matching closing bracket. We use a simple regex to find the outermost
        # array/object; this is pragmatic and works for typical model outputs.
        m = re.search(r"(\[.*\]|\{.*\})", text, re.DOTALL)
        if not m:
            return None

        snippet = m.group(1)
        try:
            return json.loads(snippet)
        except Exception:
            return None

    def annotate_image(
        self,
        prompt: str,
        image_path: Optional[str] = None,
        image_bytes: Optional[bytes] = None,
    ) -> Dict[str, Any]:
        """Send the image and prompt to the model and return the result.

        Returns a dict containing at least the key 'raw_text'. If the model
        response contains JSON, the parsed JSON is returned under the key
        'json'. Example return:
          {'raw_text': '...', 'json': [{'point':[100,200], 'label':'...'}]}

        One of `image_path` or `image_bytes` must be provided.
        """
        if image_bytes is None and image_path is None:
            raise ValueError("Either image_path or image_bytes must be provided")

        if image_bytes is None and image_path is not None:
            image_bytes = self.load_image(image_path)

        # Build contents as in the original snippet: image part then prompt
        image_part = types.Part.from_bytes(data=image_bytes, mime_type="image/png")
        contents = [image_part, prompt]

        config = types.GenerateContentConfig(
            temperature=self.temperature,
            thinking_config=types.ThinkingConfig(thinking_budget=self.thinking_budget),
        )

        response = self.client.models.generate_content(
            model=self.model_id,
            contents=contents,
            config=config,
        )

        # The object returned by the SDK typically exposes `.text`.
        raw_text = getattr(response, "text", None)
        if raw_text is None:
            # Fallback to str() if `.text` is not present
            raw_text = str(response)

        result: Dict[str, Any] = {"raw_text": raw_text}

        parsed = self._extract_json_from_text(raw_text)
        if parsed is not None:
            result["json"] = parsed

        return result


def _default_prompt() -> str:
    return (
        "Point to no more than 10 items in the image. The label returned "
        "should be an identifying name for the object detected. "
        "The answer should follow the json format: [{\"point\": <point>, "
        "\"label\": <label1>}, ...]. The points are in [y, x] format "
        "normalized to 0-1000."
    )


if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser(description="Annotate an image with Gemini Robotics model")
    parser.add_argument("image", nargs="?", default=None, help="Path to image file or a directory containing images")
    parser.add_argument("--prompt", dest="prompt", help="Prompt text to send to the model")
    parser.add_argument("--prompt-file", dest="prompt_file", help="Path to a file containing the prompt")
    parser.add_argument("--api-key", dest="api_key", help="GenAI API key (or set GENAI_API_KEY)")
    parser.add_argument("--model", dest="model", default=DEFAULT_MODEL_ID, help="Model id to use")
    parser.add_argument("--temperature", dest="temperature", type=float, default=0.5)
    parser.add_argument("--thinking-budget", dest="thinking_budget", type=int, default=0)
    args = parser.parse_args()

    # Try to read from provided arg, then environment, then .env (if python-dotenv is available),
    # then prompt securely with getpass.
    api_key = args.api_key or os.environ.get("GENAI_API_KEY")
    if not api_key:
        # Attempt to load from .env if python-dotenv is installed (don't require it)
        try:
            from dotenv import load_dotenv

            # Load .env from repository root if present
            repo_root = os.path.dirname(os.path.dirname(__file__))
            dotenv_path = os.path.join(repo_root, ".env")
            if os.path.exists(dotenv_path):
                load_dotenv(dotenv_path)
                api_key = os.environ.get("GENAI_API_KEY")
        except Exception:
            # dotenv not installed or failed; ignore silently
            api_key = api_key

    if not api_key:
        # Prompt for API key without echo
        try:
            api_key = getpass.getpass("Enter your GenAI API key (input hidden): ").strip()
        except Exception:
            # Fallback to visible input if getpass fails in this environment
            api_key = input("Enter your GenAI API key: ").strip()

    annotator = GeminiImageAnnotator(
        api_key=api_key,
        model_id=args.model,
        temperature=args.temperature,
        thinking_budget=args.thinking_budget,
    )

    # Resolve prompt: precedence: --prompt, --prompt-file, interactive -> default
    if args.prompt:
        prompt = args.prompt
    elif args.prompt_file:
        if not os.path.isfile(args.prompt_file):
            print(f"Error: prompt file not found: {args.prompt_file}")
            raise SystemExit(2)
        with open(args.prompt_file, "r", encoding="utf-8") as pf:
            prompt = pf.read()
    else:
        try:
            user_prompt = input("Enter prompt (leave empty to use default): ")
        except Exception:
            user_prompt = ""
        prompt = user_prompt.strip() or _default_prompt()

    # Resolve image path: CLI arg or interactive input (accept file or directory)
    image_path = args.image
    if not image_path:
        try:
            image_path = input("Enter image file or directory path: ").strip()
        except Exception:
            image_path = None

    if not image_path:
        print("Error: no image path provided")
        raise SystemExit(2)

    # If a directory is provided, find image files inside and allow selection
    if os.path.isdir(image_path):
        exts = ('.jpg', '.jpeg', '.png', '.bmp', '.gif', '.webp', '.tiff')
        candidates = [os.path.join(image_path, f) for f in sorted(os.listdir(image_path)) if f.lower().endswith(exts)]
        if not candidates:
            print(f"Error: no image files found in directory: {image_path}")
            raise SystemExit(2)
        if len(candidates) == 1:
            chosen = candidates[0]
            print(f"Found one image in directory, using: {chosen}")
            image_path = chosen
        else:
            print("Multiple image files found:")
            for i, p in enumerate(candidates):
                print(f"  [{i}] {os.path.basename(p)}")
            try:
                sel = input(f"Enter index of image to use [0]: ").strip()
            except Exception:
                sel = ''
            if sel == '':
                idx = 0
            else:
                try:
                    idx = int(sel)
                except Exception:
                    print("Invalid selection, defaulting to 0")
                    idx = 0
            idx = max(0, min(idx, len(candidates)-1))
            image_path = candidates[idx]

    # Basic image validation before sending
    if not os.path.isfile(image_path):
        print(f"Error: image file not found: {image_path}")
        raise SystemExit(2)

    size = os.path.getsize(image_path)
    if size == 0:
        print(f"Error: image file is empty: {image_path}")
        raise SystemExit(2)

    # Check image type if possible
    kind = imghdr.what(image_path) if imghdr is not None else None
    if kind is None:
        # imghdr couldn't detect type; fall back to mimetypes
        mtype, _ = mimetypes.guess_type(image_path)
        print(f"Warning: could not detect image type via imghdr; guessed mime: {mtype}")
    else:
        print(f"Detected image type: {kind}")

    try:
        result = annotator.annotate_image(prompt=prompt, image_path=image_path)
    except Exception as e:
        print(f"Error: {e}")
        raise

    # Nicely print results
    print("--- Raw model output ---")
    print(result.get("raw_text"))
    if "json" in result:
        print("--- Parsed JSON ---")
        print(json.dumps(result["json"], indent=2))
