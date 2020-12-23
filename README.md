**Spreadsheets Serialization** allows you to map your data to Google Spreadsheets. Made for both study & fun.
* Serialization and deserialization of any user-made class. No limitations on class nesting;
* Mark-up uses a small number of attributes, no code required. The mark-up is valid for any context;
* Deal with _ICollection<>_ types and non-generic array types using an _ArrayAttribute_;
* The result is configurable and human-readable;
* Ranges for reading & writing are automatically calculated from the context;

How to write:
* Add **using Mimimi.SpreadsheetsSerialization**
* Create a new request with **var request = new CustomBatchUpdateRequest (string spreadsheetID)**
* Add instances of your marked up classes with **request.Add\<T\> (T instance)**
* Execute the request with **request.Enqueue ()** - with optional _Action_ callback

How to read:
* Add **using Mimimi.SpreadsheetsSerialization**
* Create a new request with **var request = new CustomBatchGetRequest (string spreadsheetID)**
* Specify a requested data with **request.Add \<T\> (Action\<T\> callback)** - callback returns a value to caller
* Execute the request with **request.Enqueue ()**

Mark up classes in 2 steps:

First, add a **MapAttribute** to every Field of the class you want to serialize. **MapAttribute** has a required _index_ parameter (bigger = to the right) and an optional _group_ paremeter (bigger = downward, default is 0).
If the field type is a collection, add an **ArrayAttribute** for each dimension of it. **ArrayAttribute** has a required _direction_ parameter, which specifies the position of the next collection element relative to the previous one.
**NOTE**. When the collection field has an _ArrayAttribute_, the serializer views it as a set of instances of GenericTypeArguement type of the collection. (repeats for each _ArrayAttribute_)

Then, choose an amount of space taken by an instance of given type, and add it to the class definition. Your options are:
* **[SheetsGroupAttribute]** _SheetsGroup_ is for the largest classes. This attribute allows a single instance to be mapped on multiple sheets. _SheetsGroup_ may include fields of any type. 
* **[SheetAttribute]** _Sheet_ represents a single sheet, and for obvious reasons it can't contain fields of Sheet or SheetsGroup classes. Any number of _Range_ and _SingleValue_ fields is allowed.
* **[RangeAttribute]** Every _Range_ class occupies a rectangle of cells of any size. _Range_ can contain only fields of either _Range_ or _SingleValue_ classes.
* Any type with no class space attribute is _SingleValue_. It's mapped to a single cell and may contain to 

Namespace _Mimimi.SpreadsheetsSerialization_ contains all attributes mentioned above. Also, try inspecting an 'example' object at the scene, and an 'ExampleComponent' class. 

**NOTE**. In order to use Google API, you have to initialize the serialization service with a **service account key**. If you don't have one, Google Spreadsheets starter guide should describe the way to create it.

**NOTE**. Core functionality does not depend on _UnityEngine_. Remove dependencies on Unity:
* Every invokation of _UnityEngine.Debug.Assert_
* _ExampleComponent_ and _ExampleTargetComponent_ classes
