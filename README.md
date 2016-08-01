# GennyMcGenFace - C# Unit Test Generator

When creating a unit test have you end up spending more time writing boilerplate code to perform your test than you spend actually writing.

GennyMcGenFace's goal is to save you time when writing unit tests!

Allows you to generate random values for classes in your project.

1. Generate unit tests for each function in your class
2. Figures out valid randomly generated values for the paramater inputs and the returns statement.
3. Mockable interfaces return valid randomly generated values
4. Imports all the needed namespaces into your test class

![Toolbar](http://i.imgur.com/DfDCozg.png) 

Generating random values for a class example:

The dropdown displays a list of all the classes in your solution.
You have the option to change how many words go into string and how big the random numbers will be.

![Generated Unit Test Example](http://i.imgur.com/OPEtVdK.png)

The generated tests will need Nsubstitute nuget package if interfaces are used.

Install-Package NSubstitute



GennyMcGenFace's goal is not to generate 100% of the unit test, but to automate as much as possible.
