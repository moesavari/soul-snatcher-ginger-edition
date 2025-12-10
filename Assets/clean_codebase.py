import os
import re
import shutil
from pathlib import Path

# === CONFIG ===
CODE_ROOT = Path("Code")            # folder containing all your .cs scripts
OUTPUT_ROOT = Path("Code_Cleaned")  # where cleaned scripts will go
UNUSED_REPORT = OUTPUT_ROOT / "UNUSED_SCRIPTS_REPORT.txt"

# === STEP 1: COLLECT FILES ===
cs_files = [p for p in CODE_ROOT.rglob("*.cs")]

print(f"Found {len(cs_files)} C# files to process.")

file_contents = {}
for path in cs_files:
    text = path.read_text(encoding="utf-8", errors="ignore")
    file_contents[path] = text

# === STEP 2: DETECT CLASS/STRUCT DEFINITIONS & USAGE ===
class_pattern = re.compile(r"\b(class|struct)\s+([A-Za-z_][A-Za-z0-9_]*)")

class_to_files = {}
for path, code in file_contents.items():
    for m in class_pattern.finditer(code):
        cls = m.group(2)
        class_to_files.setdefault(cls, set()).add(path)

all_text = "\n".join(file_contents.values())

class_usage_counts = {}
for cls, defs in class_to_files.items():
    pattern = r"\b" + re.escape(cls) + r"\b"
    matches = re.findall(pattern, all_text)
    class_usage_counts[cls] = len(matches)

unused_classes_by_file = {}
for cls, defs in class_to_files.items():
    def_count = len(defs)
    use_count = class_usage_counts.get(cls, 0)
    # If it's never referenced outside its own definition(s), mark it as "likely unused"
    if use_count <= def_count:
        for p in defs:
            unused_classes_by_file.setdefault(p, set()).add(cls)

# === STEP 3: COMMENT STRIPPING ===
def strip_comments(code: str) -> str:
    # Remove /* ... */ (multiline)
    code_no_block = re.sub(r"/\*.*?\*/", "", code, flags=re.DOTALL)

    # Remove XML doc comments ///...
    code_no_xml = re.sub(r"^[ \t]*///.*$", "", code_no_block, flags=re.MULTILINE)

    # Remove // line comments (including inline)
    def _strip_line(line: str) -> str:
        if "//" in line:
            idx = line.find("//")
            return line[:idx].rstrip()
        return line

    lines = code_no_xml.splitlines()
    stripped = [_strip_line(l) for l in lines]

    # Keep empty lines as-is (Unity line numbers stay roughly stable)
    return "\n".join(stripped)

# === STEP 4: COMPLEX METHOD ANNOTATION ===
method_pattern = re.compile(
    r"(^[ \t]*(?:public|private|protected|internal|static|virtual|override|async|sealed|extern|new|partial)[^;\n{]*\))\s*\{",
    re.MULTILINE,
)

def annotate_complex_methods(code: str, line_threshold: int = 25) -> str:
    text = code
    method_spans = []

    for m in method_pattern.finditer(text):
        sig_start = m.start(1)
        start_line = text.count("\n", 0, sig_start)

        brace_start = text.find("{", m.end(1) - 1)
        if brace_start == -1:
            continue

        depth = 0
        end_pos = None
        for i in range(brace_start, len(text)):
            ch = text[i]
            if ch == "{":
                depth += 1
            elif ch == "}":
                depth -= 1
                if depth == 0:
                    end_pos = i
                    break
        if end_pos is None:
            continue

        end_line = text.count("\n", 0, end_pos)
        line_count = end_line - start_line
        if line_count > line_threshold:
            method_spans.append((start_line, m.group(1)))

    if not method_spans:
        return code

    lines = code.splitlines()
    # Insert from bottom to top so indices don't shift
    for line_idx, signature in sorted(method_spans, key=lambda x: x[0], reverse=True):
        sig = signature.strip()
        name_match = re.search(r"([A-Za-z_][A-Za-z0-9_]*)\s*\(", sig)
        method_name = name_match.group(1) if name_match else "Method"
        indent_match = re.match(r"^(\s*)", lines[line_idx]) if line_idx < len(lines) else None
        indent = indent_match.group(1) if indent_match else ""
        comment = f"{indent}// Core logic for {method_name}. Involves multiple steps and state changes."
        lines.insert(line_idx, comment)

    return "\n".join(lines)

# === STEP 5: APPLY TRANSFORMATIONS & WRITE OUTPUT ===
if OUTPUT_ROOT.exists():
    shutil.rmtree(OUTPUT_ROOT)
OUTPUT_ROOT.mkdir(parents=True, exist_ok=True)

for path, code in file_contents.items():
    rel = path.relative_to(CODE_ROOT)
    out_path = OUTPUT_ROOT / rel
    out_path.parent.mkdir(parents=True, exist_ok=True)

    cleaned = strip_comments(code)
    annotated = annotate_complex_methods(cleaned)

    out_path.write_text(annotated, encoding="utf-8")

print(f"Cleaned code written to: {OUTPUT_ROOT}")

# === STEP 6: WRITE UNUSED SCRIPTS REPORT ===
with UNUSED_REPORT.open("w", encoding="utf-8") as rep:
    rep.write("Likely unused scripts (classes/structs only referenced where defined):\n\n")
    for path in sorted(unused_classes_by_file.keys(), key=lambda p: str(p)):
        rel = path.relative_to(CODE_ROOT)
        rep.write(f"{rel}\n")
        for cls in sorted(unused_classes_by_file[path]):
            rep.write(f"  - {cls}\n")
        rep.write("\n")

print(f"Unused script report written to: {UNUSED_REPORT}")
print("Done.")
