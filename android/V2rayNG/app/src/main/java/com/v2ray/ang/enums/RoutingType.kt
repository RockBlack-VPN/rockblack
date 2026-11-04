package com.v2ray.ang.enums

/**
 * Routing preset assets. RockBlack: the upstream China / Iran presets are replaced by the
 * RockBlack profiles (see RoutingProfileManager): WHITE = Russia, BLACK = block ads only,
 * GLOBAL = global. The routing editor imports the presets through RoutingProfileManager.
 */
enum class RoutingType(val fileName: String) {
    WHITE("custom_routing_white"),
    BLACK("custom_routing_black"),
    GLOBAL("custom_routing_global"),
}
