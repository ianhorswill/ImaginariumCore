*ImaginariumCore* is the basic parser, solver, and NL generator for the *Imaginarium*
constraint-based procedural content generation system, packaged as a DLL for use in
any Unity project.  To use it, add the following files to your Assets directory:

* The compiled `Imaginarium.dll` file from the enclosed Imaginarium project
* The `CatSAT.dll` file here, or some other copy
* The `Inflections` folder and its contents, form the Imaginarium project

Then add the following to your code:

* Set `Imaginarium.Driver.DataFiles` to the path of the folder holding the `Inflections` folder.
* Call `new Imaginarium.Ontology.Ontology("name", "generatorPath")`, where `generatorPath`
  is a path to the directory holding your generator definition (you can create and debug the 
  generators using the Imaginarium IDE)
* Call `o.CommonNoun("noun").MakeGenerator().Generate()` where `o` is the ontology, and
  `"noun"` is the name of the kind of thing to generate, e.g. "character", "monster", "cat", etc.
* The result is an object of type `Invention`.  it contains a set of `PossibleIndividual`s, which 
  are the created objects.  If you just asked for one object, it will be `i[0]`,
  where `i` is the invention you generated.  You can also get a block of text describing them all by calling the Invention's `Description` method.
* The PossibleIndividual class has the following useful members
   * `NameString()` returns the whatever name has been assigned to the individual.
   * `Description()` generates an English description of the individual.
   * `IsA(concept)` tells you if the concept (a common noun or adjective) holds of
      the individual.
   * `RelatesTo(that, verb)` tells you if this possible individual verbs that.

   So, to generate a thing and its textual description, first do:

   ```
   var o = new Ontology("name", "path");
   ```

   Then, each time you want to make something do:

   ```
   o.CommonNoun("noun").MakeGenerator().Generate().Description()
   ```

   If you know you'll want to repeatedly make the same nouns, do:

   ```
   var g = o.CommonNoun("noun").MakeGenerator();
   ```
   Which will make the generator once (making a generator is expensive).  Then you can repeatedly do:

   ```
   g.Generate().Description()
   ```

   To make instances of it.
