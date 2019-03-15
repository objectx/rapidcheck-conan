from conans import ConanFile, CMake, tools
import sys

class RapidcheckConan(ConanFile):
    name = "rapidcheck"
    version = "1.0.4"
    license = "https://github.com/emil-e/rapidcheck/blob/master/LICENSE.md"
    url = "https://github.com/objectx/rapidcheck-conan"
    description = "Please visit https://github.com/emil-e/rapidcheck"
    settings = "os", "compiler", "build_type", "arch"
    options = {"shared": [True, False],
               "enable_all_extras": [True, False],
               "enable_catch": [True, False],
               "enable_gmock": [True, False],
               "enable_gtest": [True, False],
               "enable_boost": [True, False],
               "enable_boost_test": [True, False]}
    default_options = {"shared" : False,
                       "enable_all_extras": False,
                       "enable_catch": False,
                       "enable_gmock": False,
                       "enable_gtest": False,
                       "enable_boost": False,
                       "enable_boost_test": False}
    generators = "cmake"

    def source(self):
        self.run("git clone https://github.com/emil-e/rapidcheck.git")
        self.run("cd rapidcheck && git checkout --detach 3eb9b4ff69f4ff2d9932e8f852c2b2a61d7c20d3")
        # This small hack might be useful to guarantee proper /MT /MD linkage in MSVC
        # if the packaged project doesn't have variables to set it properly
        tools.replace_in_file("rapidcheck/CMakeLists.txt", "project(rapidcheck CXX)", '''project(rapidcheck CXX)
include(${CMAKE_BINARY_DIR}/conanbuildinfo.cmake)
conan_basic_setup()''')

    def build(self):
        cmake = CMake(self)
        defs = dict()
        if self.options.enable_all_extras:
            defs["RC_INSTALL_ALL_EXTRAS"] = "YES"
        else:
            if self.options.enable_catch:
                defs["RC_ENABLE_CATCH"] = "YES"
            if self.options.enable_gmock:
                defs["RC_ENABLE_GMOCK"] = "YES"
            if self.options.enable_gtest:
                defs["RC_ENABLE_GTEST"] = "YES"
            if self.options.enable_boost:
                defs["RC_ENABLE_BOOST"] = "YES"
            if self.options.enable_boost_test:
                defs["RC_ENABLE_BOOST_TEST"] = "YES"
        cmake.configure(source_folder="rapidcheck", defs=defs)
        cmake.build()

    def package(self):
        self.copy("LICENSE*", dst="licenses", src="rapidcheck")
        self.copy("*.h", dst="include", src="rapidcheck/include")
        self.copy("*.hpp", dst="include", src="rapidcheck/include")
        for e in ["catch", "gmock", "gtest", "boost", "boost_test"]:
            f = f"rapidcheck/extras/{e}/include"
            print("f =", f, file=sys.stderr)
            self.copy("*.h", dst="include", src=f)
            self.copy("*.hpp", dst="include", src=f)
        self.copy("*rapidcheck.lib", dst="lib", keep_path=False)
        self.copy("*.dll", dst="bin", keep_path=False)
        self.copy("*.so", dst="lib", keep_path=False)
        self.copy("*.dylib", dst="lib", keep_path=False)
        self.copy("*.a", dst="lib", keep_path=False)

    def package_info(self):
        self.cpp_info.libs = tools.collect_libs(self)
