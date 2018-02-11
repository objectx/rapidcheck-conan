from conans import ConanFile, CMake, tools


class RapidcheckConan(ConanFile):
    name = "rapidcheck"
    version = "1.0.0"
    license = "BSD-2"
    url = "<Package recipe repository url here, for issues about the package>"
    description = "QuickCheck clone for C++ with the goal of being simple to use with as little boilerplate as possible."
    settings = "os", "compiler", "build_type", "arch"
    options = {"shared": [True, False]}
    default_options = "shared=False"
    generators = "cmake"

    def source(self):
        self.run("git clone https://github.com/emil-e/rapidcheck.git")
        self.run("cd rapidcheck && git checkout master")
        # This small hack might be useful to guarantee proper /MT /MD linkage in MSVC
        # if the packaged project doesn't have variables to set it properly
        tools.replace_in_file("rapidcheck/CMakeLists.txt", "project(rapidcheck)", '''project(rapidcheck)
include(${CMAKE_BINARY_DIR}/conanbuildinfo.cmake)
conan_basic_setup()''')

    def build(self):
        cmake = CMake(self)
        cmake.configure(source_folder="rapidcheck")
        cmake.build()

        # Explicit way:
        # self.run('cmake %s/hello %s' % (self.source_folder, cmake.command_line))
        # self.run("cmake --build . %s" % cmake.build_config)

    def package(self):
        self.copy("LICENSE*", dst="licenses", src="rapidcheck")
        self.copy("*.h", dst="include", src="rapidcheck/include")
        self.copy("*.hpp", dst="include", src="rapidcheck/include")
        for e in ["catch", "gmock", "gtest", "boost", "boost_test"]:
            self.copy("*.h", dst="include", src=("rapidcheck/extras/%s/include" % e))
            self.copy("*.hpp", dst="include", src=("rapidcheck/extras/%s/include" % e))
        self.copy("*rapidcheck.lib", dst="lib", keep_path=False)
        self.copy("*.dll", dst="bin", keep_path=False)
        self.copy("*.so", dst="lib", keep_path=False)
        self.copy("*.dylib", dst="lib", keep_path=False)
        self.copy("*.a", dst="lib", keep_path=False)

    def package_info(self):
        self.cpp_info.libs = tools.collect_libs(self)
