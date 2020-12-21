**Spreadsheets Serialization** allows you to read & write objects to Google Spreadsheets documents. 
* Serialization and deserialization of any user-made class. No limitations on class nesting;
* Marking up a class takes a small number of attributes, no other code required. The mark-up is valid for any given context;
* Reading & writing generic collections through _ICollection<>_ interface. Non-generic arrays are supported too;
* User has enough of control over the arrangement of data in sheets. The data is written in human-readable (and editable) format;

I was working on this thing in my free time. It is a study project, so I valued experiments higher than effeciency. _It works for example cases, but I haven't put it to real use yet._

Mark up a class in 2 steps. First, add a **MapAttribute** to every Field of the class you want to serialize. If the field type is a collection, add an **ArrayAttribute** (for multi-dimensional collections, add an equal number of attributes).
**MapAttribute** has a required _index_ parameter (bigger = to the right) and an optional _group_ paremeter (bigger = downward, default is 0). 
**ArrayAttribute** has a required _direction_ parameter, which specifies the position of the next collection element relative to the previous one.
Namespace _Mimimi.SpreadsheetsSerialization_ contains all attributes mentioned.

Specify the amount of space in spreadsheets required for a single object of given type. The given class may have one of these _SpaceRequirements_:
* **[SheetsGroupAttribute]** _SheetsGroup_ is the largest one. It allows the class to map on multiple sheets. _SheetsGroup_ may contain any kind of classes. 
* **[SheetAttribute]** _Sheet_ represents a single sheet and can contain only classes of either _Range_ or _SingleValue_ sizes.
* **[RangeAttribute]** _Range_ may occupy any number of cells of the single sheet. _Range_ can contain only classes of either _Range_ or _SingleValue_ sizes.
* _SingleValue_ represents a class (or the value type) with no mark-up. It's mapped to a single cell and may contain to 
Add a corresponding attribute to the class.
Inspect the 'example' object in scene and 'ExampleComponent' class to see how it works. 

**NOTE**. In order to use Google API, you have to create a **key file**. If you don't have one, Google Spreadsheets beginner guide should describe steps to create it.

**NOTE**. Serialization scripts aren't really using _UnityEngine_. Remove dependencies on Unity with following steps:
* Remove or replace every using of _UnityEngine.Debug.Assert()_
* Remove _ExampleComponent_ and _ExampleTargetComponent_ classes
