import tkinter as tk
from tkinter import filedialog, messagebox
import subprocess
import sys
import ctypes 

def select_folder(title):
    """Opens a folder picker dialog with a custom title."""
    return filedialog.askdirectory(title=title)

def select_file(title):
    """Opens a file picker dialog with a custom title."""
    return filedialog.askopenfilename(title=title)

def on_submit():
    """Handles user input and calls the C# program with the selected paths."""
    mode = mode_var.get()
    if mode not in [1, 2, 3]:
        messagebox.showerror("Error", "Please select a valid mode.")
        return

    paths = []
    
    if mode == 1:
        paths.append(select_folder("Select Legacy Folder"))  # LegacyFolder
        paths.append(select_folder("Select Lazer \"Files\" Folder"))  # LazerFilesFolder
        paths.append(select_file("Select Realm Path File"))  # RealmPathFile
    elif mode == 2:
        paths.append(select_folder("Select Legacy Folder"))  # LazerFilesFolder
        paths.append(select_folder("Select Lazer \"Files\" Folder"))  # RealmPathFolder
    elif mode == 3:
        paths.append(select_folder("Select Legacy Folder"))  # LegacyFolder
        paths.append(select_folder("Select Lazer \"Files\" Folder"))  # LazerFilesFolder
        paths.append(select_file("Select Realm Path File"))  # RealmPathFile

    if "" in paths:  # If the user cancels any dialog, stop execution
        return

    # Ask for the numerical argument
    def get_numerical_input():
        def on_confirm():
            num_value = num_entry.get()
            if not num_value.isdigit():  # Ensure the input is a number
                messagebox.showerror("Error", "Please enter a valid numerical value.")
                return

            # Close the numerical input window
            num_window.destroy()

            # Call the C# program and pass arguments
            command = f'cmd.exe /K combolazerstorage.exe {mode} ' + " ".join(paths) + f" {num_value}"
            
            try:
                # Run the command in a new CMD window
                subprocess.Popen(command, shell=True)
            except Exception as e:
                messagebox.showerror("Error", f"Failed to run C# program: {e}")

        # Create a new window for numerical input
        num_window = tk.Toplevel(root)
        num_window.title("Enter Numerical Argument")

        tk.Label(num_window, text="Enter osu! Realm schema version:").pack()
        num_entry = tk.Entry(num_window)
        num_entry.pack()

        confirm_button = tk.Button(num_window, text="Confirm", command=on_confirm)
        confirm_button.pack()

    # Call the function to ask for numerical input
    get_numerical_input()

def is_admin():
    try:
        return ctypes.windll.shell32.IsUserAnAdmin() != 0
    except:
        return False

# If the script is not running as admin, relaunch it with admin rights
if not is_admin():
    # Arguments for relaunching the script with admin privileges
    script = sys.argv[0]
    params = " ".join(sys.argv[1:])
    
    # Use ShellExecuteEx to run the script as administrator
    ctypes.windll.shell32.ShellExecuteW(None, "runas", sys.executable, f'"{script}" {params}', None, 1)
    print("l")
    sys.exit()
# GUI setup
root = tk.Tk()
root.title("Mode Selector")

tk.Label(root, text="Select Mode:").pack()

mode_var = tk.IntVar()

tk.Radiobutton(root, text="Lazer Database to Legacy Storage: Legacy Folder, Lazer Files Folder, Realm Path File", variable=mode_var, value=1).pack()
tk.Radiobutton(root, text="Legacy Storage to Lazer Symlinks: Lazer Files Folder, Realm Path Folder", variable=mode_var, value=2).pack()
tk.Radiobutton(root, text="Legacy Storage & Symlinks to Lazer Database: Legacy Folder, Lazer Files Folder, Realm Path File", variable=mode_var, value=3).pack()

submit_button = tk.Button(root, text="Select Paths & Run", command=on_submit)
submit_button.pack()

root.mainloop()
