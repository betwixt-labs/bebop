include(Bebop.cmake)

file(READ "../../.env" env)
message(STATUS "${env}") 
string(REGEX MATCH "\"([^\"\"]*)\"" _ ${env}) 

set(BEBOPC_VERSION "${CMAKE_MATCH_1}") 

set(bop_hpp ${CMAKE_CURRENT_SOURCE_DIR}/test.g.hpp)
file(GLOB bop_files CONFIGURE_DEPENDS ../../Laboratory/Schemas/Valid/*.bop)

bebop_generate(generate_bop_hpp
    VERSION "v${BEBOPC_VERSION}"
    LANGUAGE cpp
    NAMESPACE rw::bop
    OUTPUT ${bop_hpp}
    BOPS ${bop_files}
)
