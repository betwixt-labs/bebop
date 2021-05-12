#include <variant>
#include "../gen/union.hpp"

int main() {
  WeirdOrder o;

  // We can put a value in...
  o.variant = TwoComesFirst { 42 };

  // And get it back out
  TwoComesFirst x = std::get<TwoComesFirst>(o.variant);
  if (x != 42) return 1;

  return 0;
}
