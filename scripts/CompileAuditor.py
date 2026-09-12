import os
import re
import subprocess
import sys

UNITY_CANDIDATES = [
    r"C:\Program Files\Unity\Hub\Editor\6000.0.12f1\Editor\Unity.exe",
    r"C:\Program Files\Unity\Hub\Editor\2022.3.12f1\Editor\Unity.exe",
    r"C:\Program Files\Unity\Editor\Unity.exe"
]

def find_unity_path():
    unity_env = os.environ.get("UNITY_PATH")
    if unity_env and os.path.exists(unity_env):
        return unity_env
        
    for path in UNITY_CANDIDATES:
        if os.path.exists(path):
            return path
            
    hub_editors_path = r"C:\Program Files\Unity\Hub\Editor"
    if os.path.exists(hub_editors_path):
        for root, dirs, files in os.walk(hub_editors_path):
            if "Unity.exe" in files:
                return os.path.join(root, "Unity.exe")
                
    return None

def main():
    unity_path = find_unity_path()
    if not unity_path:
        print("[CompileAuditor] Error: Unity.exe could not be found. Please set UNITY_PATH environment variable.")
        sys.exit(1)
        
    # Project Root (Where scripts/ parent is located)
    project_path = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
    log_file = os.path.join(project_path, "build.log")
    
    print(f"[CompileAuditor] Unity Path Found: {unity_path}")
    print(f"[CompileAuditor] Auditing compilation for project: {project_path} ...")
    
    cmd = [
        unity_path,
        "-batchmode",
        "-projectPath", project_path,
        "-executeMethod", "UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation",
        "-quit",
        "-logFile", log_file
    ]
    
    try:
        subprocess.run(cmd, check=True)
    except subprocess.CalledProcessError:
        pass
        
    if not os.path.exists(log_file):
        print(f"[CompileAuditor] Log file not found at {log_file}. Compile check failed.")
        sys.exit(1)
        
    error_pattern = re.compile(r"^(.*?)\((\d+),(\d+)\):\s+(error\s+CS\d+:\s+.*)$")
    
    errors = []
    with open(log_file, "r", encoding="utf-8", errors="ignore") as f:
        for line in f:
            match = error_pattern.match(line.strip())
            if match:
                file_path, row, col, msg = match.groups()
                errors.append({
                    "file": file_path,
                    "line": int(row),
                    "col": int(col),
                    "message": msg
                })
                
    if len(errors) > 0:
        print(f"\n[CompileAuditor] Found {len(errors)} compilation errors:")
        for err in errors:
            print(f"  - File: {err['file']} (Line {err['line']}, Col {err['col']})")
            print(f"    Message: {err['message']}\n")
        sys.exit(1)
    else:
        print("\n[CompileAuditor] Success: Compilation completed with 0 errors!")
        sys.exit(0)

if __name__ == "__main__":
    main()
