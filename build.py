#!/usr/bin/env python3
"""
Simple build helper

Runs `npm` build in the `frontend` folder and copies the generated
`dist` output into `Chatter/wwwroot` (replacing its contents).

Usage: `python build.py`
"""
from pathlib import Path
import subprocess
import sys
import shutil
import os


def run(cmd, cwd=None):
    # Ensure command parts are strings for printing/joining
    safe_cmd = [str(c) for c in cmd] if isinstance(cmd, (list, tuple)) else str(cmd)
    print(f"> {' '.join(safe_cmd) if isinstance(safe_cmd, (list, tuple)) else safe_cmd} (cwd={cwd})")
    try:
        subprocess.run(cmd, cwd=str(cwd) if cwd is not None else None, check=True)
    except FileNotFoundError as e:
        # Give a clearer message when the executable isn't found (common on Windows)
        exe = cmd[0] if isinstance(cmd, (list, tuple)) and cmd else cmd
        print(f"Executable not found: {exe}. Ensure it is installed and on PATH.")
        raise


def main():
    repo_root = Path(__file__).resolve().parent
    frontend = repo_root / "frontend"
    target = repo_root / "Chatter" / "wwwroot"

    if not frontend.exists():
        print(f"Frontend directory not found: {frontend}")
        sys.exit(1)

    # Install dependencies if a lockfile exists, otherwise skip to build.
    pkg_lock = frontend / "package-lock.json"
    pkg_json = frontend / "package.json"

    # Ensure npm is available
    def find_executable(name: str):
        # Try normal lookup, then Windows-style extensions
        path = shutil.which(name)
        if path:
            return path
        if os.name == "nt":
            for ext in (".cmd", ".exe", ".bat"):
                path = shutil.which(name + ext)
                if path:
                    return path
        return None

    npm_path = find_executable("npm")
    if not npm_path and (pkg_lock.exists() or pkg_json.exists()):
        print("`npm` executable not found. Please install Node.js and ensure `npm` is on your PATH.")
        sys.exit(2)

    try:
        # Only install dependencies if node_modules is missing. This avoids
        # running `npm install` on every build which is slow.
        node_modules = frontend / "node_modules"
        if (pkg_lock.exists() or pkg_json.exists()) and not node_modules.exists():
            if pkg_lock.exists():
                print("Installing dependencies with `npm ci`...")
                run([npm_path or "npm", "ci"], cwd=str(frontend))
            else:
                print("Installing dependencies with `npm install`...")
                run([npm_path or "npm", "install"], cwd=str(frontend))
        else:
            print("Dependencies already installed; skipping install step.")

        print("Building frontend (npm run build)...")
        run([npm_path or "npm", "run", "build"], cwd=str(frontend))
    except subprocess.CalledProcessError as e:
        print(f"Command failed with exit code {e.returncode}")
        sys.exit(e.returncode)

    dist = frontend / "dist"
    if not dist.exists():
        print(f"Expected build output not found at: {dist}")
        sys.exit(1)

    # Ensure target directory exists, then remove its contents (but keep the dir).
    if not target.exists():
        target.mkdir(parents=True)
    else:
        if target.is_file() or target.is_symlink():
            print(f"Removing existing target file and recreating directory: {target}")
            try:
                target.unlink()
            except Exception as e:
                print(f"Failed to remove file {target}: {e}")
                print("Try running the script with elevated permissions or ensure the file is not in use.")
                sys.exit(1)
            target.mkdir(parents=True)
        else:
            print(f"Removing contents of existing target directory: {target}")

            def _on_rm_error(func, path, exc_info):
                # Attempt to fix permission issues then retry
                try:
                    os.chmod(path, 0o700)
                    func(path)
                except Exception as e:
                    print(f"Failed to remove {path} even after chmod: {e}")
                    raise

            def remove_path(p: Path):
                try:
                    if p.is_dir() and not p.is_symlink():
                        shutil.rmtree(p, onerror=_on_rm_error)
                    else:
                        p.unlink()
                except PermissionError as e:
                    try:
                        os.chmod(p, 0o700)
                        if p.exists():
                            if p.is_dir() and not p.is_symlink():
                                shutil.rmtree(p, onerror=_on_rm_error)
                            else:
                                p.unlink()
                    except Exception as e2:
                        print(f"Permission error removing {p}: {e2}")
                        raise

            # Iterate and remove each child safely. Catch permission errors when listing.
            try:
                children = list(target.iterdir())
            except PermissionError as e:
                print(f"Permission denied listing directory {target}: {e}")
                print("Make sure no process is locking the directory (IIS, dotnet run, etc.) and run with sufficient permissions.")
                sys.exit(1)

            for child in children:
                try:
                    remove_path(child)
                except Exception as e:
                    print(f"Warning: failed to remove {child}: {e}")
                    print("If files are in use by another process (e.g. IIS, dotnet run), stop that process and try again.")
                    print("You may also need to run this script with elevated permissions on Windows.")
                    sys.exit(1)

    print(f"Copying build from {dist} to {target}")
    try:
        # Copy into existing directory when supported.
        shutil.copytree(dist, target, dirs_exist_ok=True)
    except TypeError:
        # Older Python where `dirs_exist_ok` isn't available: copy contents manually.
        try:
            for child in dist.iterdir():
                dest = target / child.name
                if child.is_dir():
                    shutil.copytree(child, dest)
                else:
                    shutil.copy2(child, dest)
        except Exception as e:
            print(f"Failed to copy build files: {e}")
            sys.exit(1)
    except Exception as e:
        print(f"Failed to copy build files: {e}")
        sys.exit(1)

    # Print a short summary
    file_count = sum(1 for _ in target.rglob("*") if _.is_file())
    print(f"Copied {file_count} files to {target}")
    print("Build + copy completed successfully.")


if __name__ == "__main__":
    main()
