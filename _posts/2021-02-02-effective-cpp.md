---
layout: post
title: Effective C++ 笔记
enable: true
---

## Accustoming Yourself to C++

### Item 1: View C++ as a federation of languages.

**Things to Remember**

- Rules for effective C++ programming vary, depending on the part of C++ you are using.

### Item 2: Prefer consts, enums, and inlines to #defines.

**Things to Remember**

- For simple constants, prefer const objects or enums to #defines.
- For function-like macros, prefer inline functions to #defines.

### Item 3: Use const whenever possible.

**Things to Remember**

- Declaring something const helps compilers detect usage errors. const can be applied to objects at any scope, to function parameters and return types, and to member functions as a whole.
- Compilers enforce bitwise constness, but you should program using logical constness.
- When const and non-const member functions have essentially identical implementations, code duplication can be avoided by having the non-const version call the const version.

### Item 4: Make sure that objects are initialized before they're used.

**Things to Remember**

- Manually initialize objects of build-in type, because C++ only sometimes initializes them itself.
- In a constructor, prefer use of the member initialization list to assignment inside the body of the constructor. List data members in the initialization list in the same order they're declared in the class.
- Avoid initialization order problems across translation units by replacing non-local static objects with local static objects.

## Constructors, Destructors, and Assignment Operators

### Item 5: Know what functions C++ silently writes and calls.

**Things to Remember**

- Compilers may implicitly generate a class's default constructor, copy constructor, copy assignment operator, and destructor.

### Item 6: Explicitly disallow the use of compiler-generated functions you do not want.

**Things to Remember**

- To disallow functionality automatically provided by compilers, declare the corresponding member functions private and give no implementations. Using a base class like Uncopyable is one way to do this.

### Item 7: Declare destructors virtual in polymorphic base classes.

**Things to Remember**

- Polymorphic base classes should declare virtual destructors. If a class has any virtual functions, it should have a virtual destructor.
- Classes not designed to be base classes or not designed to be used polymorphically should not declare virtual destructors.

### Item 8: Prevent exceptions from leaving destructors.

**Things to Remember**

- Destructors should never emit exceptions. If functions called in a destructor may throw, the destructor should catch any exceptions, then swallow them or terminate the program.
- If class clients need to be able to react to exceptions thrown during an operation, the class should provide a regular (i.e., non-destructor) function that performs the operation.

### Item 9: Never call virtual functions during construction or destruction.

**Things to Remember**

- Don't call virtual functions during construction or destruction, because such calls will never go to a more derived class than that of the currently executing constructor or destructor.

### Item 10: Have assignment operators return a reference to *this.

**Things to Remember**

- Have assignment operators return a reference to *this.

### Item 11: Handle assignment to self in operator=.

**Things to Remember**

- Make sure operator= is well-behaved when an object is assigned to itself. Techniques include comparing addresses of source and target objects, careful statement ordering, and copy-and swap.
- Make sure that any function operating on more than one object behaves correctly if two or more of the objects are the same.

### Item 12: Copy all parts of an object.

**Things to Remember**

- Copying functions should be sure to copy all of an object's data members and all of its base class parts.
- Don't try to implement one of the copying functions in terms of the other. Instead, put common functionality in a third function that both call.

## Resource Management

### Item 13: Use objects to manage resources.

**Things to Remember**

- To prevent resource leaks, use RAII objects that acquire resources in their constructors and release them in their destructors.
- Two commonly useful RAII classes are tr1::shared_ptr and auto_ptr. tr1::shared_ptr is usually the better choice, because its behavior when copied is intuitive. Copying an auto_ptr sets it to null.

### Item 14: Think carefully about copying behavior in resource-managing classes.

**Things to Remember**

- Copying an RAII object entails copying the resource it manages, so the copying behavior of the resource determines the copying behavior of the resource determines the copying behavior of the RAII object.
- Common RAII class copying behaviors are disallowing copying and performing reference counting, but other behaviors are possible.

### Item 15: Provide access to raw resources in resource-managing classes.

**Things to Remember**

- APIs often require access to raw resources, so each RAII class should offer a way to get at the resource it manages.
- Access may be via explicit conversion or implicit conversion. In general, explicit conversion is safer, but implicit conversion is more convenient for clients.

### Item 16: Use the same form in corresponding uses of new and delete.

**Things to Remember**

- If you use [] in a new expression, you must use [] in the corresponding delete expression. If you don't use [] in a new expression, you mustn't use [] in the corresponding delete expression.

### Item 17: Store newed objects in smart pointers in standalone statements.

**Things to Remember**

- Store newed objects in smart pointers in standalone statements. Failure to do this can lead to subtle resource leaks when exceptions are thrown.




