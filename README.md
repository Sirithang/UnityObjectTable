# UnityObjectTable

Unity Editor window that display all prefab or scriptable object of a given type in editable table form

It use unity TreeView control to draw the table, and SerializedObject to handle drawing the property, so it should hand undo/redo and property drawer.

It work on all Monobehaviour subclass and on all ScriptableObject subclass in your project.

# Installation

Copy the ObjectTableWindow.cs in an Editor folder in the project.

# Usage

![GIF of usage](https://i.imgur.com/NtAasmN.gif)

Can be opened through the `ObjectTable/Open` menu entry in the menu bar.

In the Type dropdown select which Type you want to edit. The list will then be populated with all the instances of that scriptable object type or all prefab having the given Monobheaviour on it.

Some column for type like int/float/string... can be sorted by clicking on them.

Scriptable object will have a **New** button available to create a new instance of that type. The new instance will be placed in the root Assets folder if no folder is picked or in the choosen folder otherwise.

# Limitation

- Large number of prefab may make the system slow (du to how we find prefab containing the given Monobehaviour subclass). Need more test.
- Deleting (pressing Delete with row selected) is still a buggy and may requie to close & reopen the window.
- Array aren't supported. unsure what is the best UX and UI for them yet.... 
