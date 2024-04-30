---
layout: post
title: Effective C++ 笔记
enable: true
---

## Accustoming Yourself to C++

### Item 2: Prefer consts, enums, and inlines to #defines.

### Item 3: Use const whenever possible.

### Item 4: Make sure that objects are initialized before they're used.

**Things to Remember**
- Manually initialize objects of build-in type, because C++ only sometimes initializes them itself.
- In a constructor, prefer use of the member initialization list to assignment inside the body of the constructor. List data members in the initialization list in the same order they're declared in the class.
- Avoid initialization order problems across translation units by replacing non-local static objects with local static objects.

## Constructors, Destructors, and Assignment Operators

### Know what functions C++ silently writes and calls.

**Things to Remember**
- Compilers may implicitly generate a class's default constructor, copy constructor, copy assignment operator, and destructor.



