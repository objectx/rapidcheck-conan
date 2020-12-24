from conans import ConanFile, CMake, tools
from conans.errors import ConanInvalidConfiguration
import sys, os


class RapidcheckConan(ConanFile):
    name = "rapidcheck"
    description = "Please visit https://github.com/emil-e/rapidcheck"
    version = "1.0.9"
    url = "https://github.com/objectx/rapidcheck-conan"
    homepage = "http://github.com/emil-e/rapidcheck"
    topics = ("conan", "rapidcheck", "testing", "property-based-testing", "quickcheck")
    license = "https://github.com/emil-e/rapidcheck/blob/master/LICENSE.md"
    exports_sources = ["CMakeLists.txt"]
    generators = "cmake", "cmake_find_package"
    settings = "os", "compiler", "build_type", "arch"
    options = {
        "shared": [True, False],
        "fPIC": [True, False],
        "enable_rtti": [True, False],
    }
    default_options = {
        "shared": False,
        "fPIC": True,
        "enable_rtti": True,
    }

    _cmake = None

    @property
    def _source_subfolder(self):
        return "source_subfolder"

    def config_options(self):
        if self.settings.os == "Windows":
            del self.options.fPIC

    def configure(self):
        pass

    def requirements(self):
        pass

    def source(self):
        self.run("git clone https://github.com/emil-e/rapidcheck.git")
        self.run(
            "cd rapidcheck && git checkout --detach b78f89288c7e086d06e2a1e10b605d8375517a8a"
        )
        os.rename("rapidcheck", self._source_subfolder)

    def _configure_cmake(self):
        if self._cmake:
            return self._cmake
        self._cmake = CMake(self)
        self._cmake.definitions["RC_INSTALL_ALL_EXTRAS"] = True
        self._cmake.definitions["RC_ENABLE_RTTI"] = not self.options.enable_rtti
        self._cmake.configure()
        return self._cmake

    def _disable_werror(self):
        tools.replace_in_file(
            os.path.join(self._source_subfolder, "cmake", "utils.cmake"), "/WX", ""
        )

    def build(self):
        cmake = self._configure_cmake()
        cmake.build()

    def package(self):
        self.copy("LICENSE*", dst="licenses", src=self._source_subfolder)
        cmake = self._configure_cmake()
        cmake.install()
        tools.rmdir (os.path.join(self.package_folder, "share"))

    def package_id(self):
        pass

    def package_info(self):
        component_name = "librapidcheck"
        self.cpp_info.components[component_name].libs = tools.collect_libs(self)

        if not self.options.enable_rtti:
            self.cpp_info.components[component_name].defines.append("RC_DONT_USE_RTTI=1")
